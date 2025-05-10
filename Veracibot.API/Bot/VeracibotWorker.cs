
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OpenAI;
using System.Net.Http.Headers;
using System.Xml.Linq;
using Veracibot.API.Data;
using Veracibot.API.Models;

namespace Veracibot.API.Bot
{
    public class VeracibotTweetWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public VeracibotTweetWorker(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VeracibotDbContext>();
                var veracibotOptions = scope.ServiceProvider.GetRequiredService<IOptions<VeracibotOptions>>().Value;
                var tweetOptions = scope.ServiceProvider.GetRequiredService<IOptions<TweeterOptions>>().Value;
                var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var openAiOptions = scope.ServiceProvider.GetRequiredService<IOptions<OpenAIOptions>>().Value;
                //var client = new TwitterClient( tweetOptions.AccessToken, tweetOptions.ClientSecret, tweetOptions.BearerToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var lastTweetDate = await dbContext.Tweets.MaxAsync(x => x.Date);
                        using var client = httpFactory.CreateClient();
                        {
                            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tweetOptions.BearerToken);

                            string mentionsUrl = $"https://api.twitter.com/2/users/{tweetOptions.UserId}/mentions?tweet.fields=author_id,created_at,text&start_time={lastTweetDate.ToString("yyyy-MM-ddTHH:mm:ssZ")}";
                            var response = await client.GetAsync(mentionsUrl);
                            string responseContent = await response.Content.ReadAsStringAsync();

                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Erro: {response.StatusCode}\n{responseContent}");
                                return;
                            }

                            var json = JObject.Parse(responseContent);
                            var tweets = json["data"];

                            // Só a partir de agora

                            if (tweets is not null)
                            {
                                Console.WriteLine("Treating mentions since " + lastTweetDate);

                                foreach (var tweet in tweets)
                                {

                                    string tweetId = tweet["id"].ToString();
                                    string authorId = tweet["author_id"].ToString();

                                    if (authorId == tweetOptions.UserId)
                                    {
                                        Console.WriteLine($"Tweet {tweetId} is from the bot itself.");
                                        continue;
                                    }

                                    string tweetDate = tweet["created_at"].ToString();

                                    if (!String.IsNullOrWhiteSpace(tweetOptions.RestrictUserId))
                                    if (authorId != tweetOptions.RestrictUserId)
                                    {
                                        Console.WriteLine($"Tweet {tweetId} not authorized.");
                                        continue;
                                    }

                                    if (await dbContext.Tweets.AnyAsync(e => e.Id == tweetId))
                                    {
                                        Console.WriteLine($"Tweet {tweetId} already processed.");
                                        continue;
                                    }

                                    Console.WriteLine($"Getting full thread {tweetId}...");



                                    TweeterService.ThreadContext fullThread = await TweeterService.GetThreadContext(tweetId, authorId, client, tweetOptions);
                                    if (fullThread != null && fullThread.AuthorA != tweetOptions.UserId)
                                    {

                                        // Se for só um tweet, não precisa de thread, só fale a resposta    

                                        if (fullThread.Tweets.Count == 1)
                                        {

                                            Console.WriteLine($"Tweet {tweetId} is a single tweet.");

                                            var helpTweet = new Tweets()
                                            {
                                                Id = tweetId,
                                                OriginalText = "",
                                                ThreadId = fullThread.Id,
                                                Text = "",
                                                AuthorId = authorId,
                                                OriginalAuthorId = fullThread.AuthorA,
                                                 TweetVeracity = ETweetVeracity.Neutro,
                                                  Date = DateTime.UtcNow
                                            };

                                            dbContext.Tweets.Add(helpTweet);
                                            dbContext.SaveChanges();

                                            string helpImage = "img/logo.jpg";
                                            string helpResponse = "Esse é o VERACIBOT, seu robô para verificação de fatos aprovado pelo Ministério da Verdade. Para saber mais detalhes sobre o projeto consulte https://veraci.bot";

                                            TweeterService.TwitterUser twitterUser = await TweeterService.GetTwitterUserById(authorId, client, tweetOptions);
                                            TweetAuthor authorTweet = await TweeterService.GetTweetAuthor(dbContext, authorId, twitterUser.Username, twitterUser.Name);

                                            helpResponse = helpResponse + "\n\n [ESCREVER O SALDO DO AUTHOR]";

                                            await TweeterService.PostReplyWithImageAsync(helpResponse, helpImage, tweetId, client,tweetOptions);

                                            continue;

                                        }

                                        // Não responder a mesma thread

                                        var previousThread = await dbContext.Tweets.FirstOrDefaultAsync(e => e.ThreadId == fullThread.Id);
                                        if (previousThread != null)
                                        {
                                            Console.WriteLine($"Thread {tweetId} already processed.");
                                            continue;
                                        }

                                        // Checa se tem crédito

                                        TweeterService.TwitterUser userAuthorA = await TweeterService.GetTwitterUserById(fullThread.AuthorA, client, tweetOptions);
                                        TweeterService.TwitterUser userAuthorB = await TweeterService.GetTwitterUserById(fullThread.AuthorB, client, tweetOptions);

                                        TweetAuthor authorA = await TweeterService.GetTweetAuthor(dbContext, fullThread.AuthorA, userAuthorA.Username, userAuthorA.Name);
                                        TweetAuthor authorB = await TweeterService.GetTweetAuthor(dbContext, fullThread.AuthorB, userAuthorB.Username, userAuthorB.Name);

                                        fullThread.AuthorA = authorA.UserName;
                                        fullThread.AuthorB = authorB.UserName;


                                        //if (authorB.Value <= 0)
                                        //{

                                        //    Console.WriteLine($"Author {authorB.UserName} do not have credit.");

                                        //    VeraciBot.Data.Tweet notTweet = new Data.Tweet()
                                        //    {
                                        //        Id = tweetId,
                                        //        OriginalText = "",
                                        //        ThreadId = fullThread.Id,
                                        //        Text = "",
                                        //        AuthorId = authorId,
                                        //        OriginalAuthorId = fullThread.AuthorA,
                                        //        Result = 0
                                        //    };

                                        //    dbContext.Tweets.Add(notTweet);
                                        //    dbContext.SaveChanges();

                                        //    string notImgem = "img/nao.jpg";
                                        //    string notResponse = "Você não tem crédito para usar o VERACIBOT, precisa se comportar melhor! sinto muito!";

                                        //    notResponse = notResponse + "\n\n" + authorB.GetDescription();

                                        //    await TwitterAPI.PostReplyWithImageAsync(notResponse, notImgem, tweetId);

                                        //    continue;

                                        //}

                                        // Chama o CHAT GPT

                                       var result = await OpenAIService.CheckThread(fullThread, openAiOptions);
                                        if (result == null)
                                        {
                                            Console.WriteLine($"Thread {fullThread.Id} failed to check.");
                                            continue;
                                        }

                                        // Prepara a resposta
                                        Tweets fullResponseTweet = new Tweets()
                                        {
                                            Id = tweetId,
                                            ThreadId = fullThread.Id,
                                            Text = fullThread.GetStartB(),
                                            OriginalText = fullThread.GetStartA(),
                                            AuthorId = fullThread.AuthorB,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Date = DateTime.UtcNow,
                                            TweetVeracity = result.VeracityCheckOutcome
                                             
                                        };

                                        TweetAuthor author = await TweeterService.GetTweetAuthor(dbContext, fullResponseTweet.AuthorId);
                                        TweetAuthor originalAuthor = await TweeterService.GetTweetAuthor(dbContext, fullResponseTweet.OriginalAuthorId);
                                        AuthorBalance authorBalance = await dbContext.AuthorBalances.OrderByDescending(x=>x.Id).FirstOrDefaultAsync(e => e.AuthorId == fullResponseTweet.AuthorId);
                                        AuthorBalance originalAuthorBalance = await dbContext.AuthorBalances.OrderByDescending(x => x.Id).FirstOrDefaultAsync(e => e.AuthorId == fullResponseTweet.OriginalAuthorId);
                                        
                                        var scores = veracibotOptions.ScoreConfigurations[fullResponseTweet.TweetVeracity];
                                        var newAuthorBalance = new AuthorBalance { AuthorId = author.Id, Date = DateTime.UtcNow, PreviousBalance = authorBalance.CurrentBalance, CurrentBalance = authorBalance.CurrentBalance + scores.AuthorChange, Value = scores.AuthorChange, TweetId = fullResponseTweet.Id, Type = BalanceType.TweetCheck };
                                        var newOriginalAuthorBalance = new AuthorBalance { AuthorId = originalAuthor.Id, Date = DateTime.UtcNow, PreviousBalance = originalAuthorBalance.CurrentBalance, CurrentBalance = originalAuthorBalance.CurrentBalance + scores.OriginalAuthorChange, Value = scores.OriginalAuthorChange, TweetId = fullResponseTweet.Id, Type = BalanceType.BotCall };
                                        
                                        await dbContext.Tweets.AddAsync(fullResponseTweet);
                                        var entryAuthor = await dbContext.AuthorBalances.AddAsync(newAuthorBalance);
                                        var entryOriginalAuthor = await dbContext.AuthorBalances.AddAsync(newOriginalAuthorBalance);
                                        await dbContext.SaveChangesAsync();

                                        string fullResponseImage = "img/resp" + result.VeracityCheckOutcome + ".jpg";
                                        string fullResponseText = result.Message;

                                        fullResponseText = "@" + authorA.UserName + ": " + fullResponseText + "\n\n" + $"{author.Name} (@{author.UserName}) : {entryAuthor.Entity.CurrentBalance.ToString("N1")}" + "\n" + $"{originalAuthor.Name} (@{originalAuthor.UserName}) : {entryOriginalAuthor.Entity.CurrentBalance.ToString("N1")}";

                                        await TweeterService.PostReplyWithImageAsync(fullResponseText, fullResponseImage, tweetId, client, tweetOptions);

                                    }

                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.ToString());

                    }

                    await Task.Delay(TimeSpan.FromSeconds(veracibotOptions.VeracibotTweetIntervalSeconds), stoppingToken);
                }
            }
        }
    }

    public class VeracibotBalanceWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public VeracibotBalanceWorker(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VeracibotDbContext>();
                //VeracibotOptions
                var veracibotOptions = scope.ServiceProvider.GetRequiredService<IOptions<VeracibotOptions>>().Value;
                while (!stoppingToken.IsCancellationRequested)
                {








                    await Task.Delay(TimeSpan.FromSeconds(veracibotOptions.VeracibotBalanceIntervalSeconds), stoppingToken);
                }
            }
        }
    }

    public class VeracibotOpenAIWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public VeracibotOpenAIWorker(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VeracibotDbContext>();
                //VeracibotOptions
                var veracibotOptions = scope.ServiceProvider.GetRequiredService<IOptions<VeracibotOptions>>().Value;
                var openApiOptions = scope.ServiceProvider.GetRequiredService<IOptions<OpenAIOptions>>().Value;
                OpenAIClient _openAIClient = new OpenAIClient(openApiOptions.ApiKey);

                while (!stoppingToken.IsCancellationRequested)
                {
                    







                    await Task.Delay(TimeSpan.FromSeconds(veracibotOptions.VeracibotOpenAIIntervalSeconds), stoppingToken);
                }
            }
        }
    }
}
