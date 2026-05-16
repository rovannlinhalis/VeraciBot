using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using VeraciBot.Data;
using static System.Net.WebRequestMethods;
using static VeraciBot.TwitterAPI;


namespace VeraciBot
{

    class Program
    {

        
        static async Task Main(string[] args)
        {

            Console.WriteLine("Starting VERACIBOT");

            // Cria a tarefa e espera ela
            Task tarefa1 = ThreadCicloTwitterChatGpt();

            Console.WriteLine("As tarefas foram iniciadas...");

            // Aguarda as duas tarefas terminarem
            await Task.WhenAll(tarefa1);

            Console.WriteLine("Programa finalizado.");

        }

        static async Task ThreadCicloTwitterChatGpt()
        {

            Console.WriteLine("TWIT: Connecting VERACIBOT database");

            var services = new ServiceCollection();

            services.AddDbContext<VeraciDbContext>(options => options.UseSqlServer(AppKeys.keys.dbConnection));
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<VeraciDbContext>();

            // Cria o banco e a tabela automaticamente se não existirem
            dbContext.Database.EnsureCreated();

            Console.WriteLine("TWIT: Starting VERACIBOT bot");

            string startTime = DbConfig.GetLastDateTimeForTwitterCheck(dbContext).Result.ToString("yyyy-MM-ddTHH:mm:ssZ");

            Console.WriteLine("TWIT: Checking mentions to @veracibot since " + startTime);

            while (true)
            {

                try
                {

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

                    string mentionsUrl = $"https://api.twitter.com/2/users/{AppKeys.keys.xUserId}/mentions?tweet.fields=author_id,created_at,text&start_time={startTime}";

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

                    if (tweets == null)
                    {

                        Console.WriteLine("No mentions since " + startTime);
                        startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    }
                    else
                    {

                        Console.WriteLine("Treating mentions since " + startTime);
                        startTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                        DateTime lastTime = DateTime.Parse(startTime);

                        foreach (var tweet in tweets)
                        {

                            // Pega infos do tweet

                            if (tweet?["id"] == null)
                            {
                                Console.WriteLine("Tweet id is null, skipping.");
                                continue;
                            }
                            string tweetId = tweet["id"]!.ToString();

                            if (tweet?["author_id"] == null)
                            {
                                Console.WriteLine($"Tweet {tweetId} author_id is null, skipping.");
                                continue;
                            }
                            string authorId = tweet["author_id"]!.ToString();

                            if (tweet?["created_at"] == null)
                            {
                                Console.WriteLine($"Tweet {tweetId} created_at is null, skipping.");
                                continue;
                            }
                            string tweetDate = tweet["created_at"]!.ToString();

                            // Atualiza o último tweet processado

                            lastTime = DateTime.Parse(tweetDate);
                            DbConfig.SetLastDateTimeForTwitterCheck(dbContext, lastTime).Wait();

                            // Já tratei esse tweet? -> ignora

                            var previousTweet = await dbContext.Tweets.FirstOrDefaultAsync(e => e.Id == tweetId);
                            if (previousTweet != null)
                            {
                                Console.WriteLine($"Tweet {tweetId} already processed.");
                                continue;
                            }

                            // Usuário tem autorização?

                            var authorization = await dbContext.AuthorizedUsers.FirstOrDefaultAsync(e => e.Id == authorId);
                            if (authorization == null || (authorization != null && authorization.Status == AuthorizedUser.STATUS_NOT_AUTHORIZED))
                            {

                                // Não está autorizado

                                VeraciBot.Data.Tweet notAuthTweet = new Data.Tweet()
                                {
                                    Id = tweetId,
                                    OriginalText = "",
                                    ThreadId = tweetId,
                                    Text = "",
                                    AuthorId = authorId,
                                    OriginalAuthorId = authorId,
                                    Result = 0
                                };

                                dbContext.Tweets.Add(notAuthTweet);
                                dbContext.SaveChanges();

                                // TODO: Verificar a lingua do usuário e responder na língua correta

                                string notAuthorizedResponseText = await Phrases.GetNotAuthorizedResponseAsync("pt");

                                await TwitterAPI.PostReplyWithImageAsync(notAuthorizedResponseText, Images.notAuthorizedImage, tweetId);

                                continue;

                            }

                            // Tenho que tratar esse tweet

                            Console.WriteLine($"Getting full thread {tweetId}...");

                            // Pega todo o contexto da thread que o tweet faz parte

                            TwitterAPI.ThreadContext fullThread = await TwitterAPI.GetThreadContext(tweetId, authorId);
                            if (fullThread != null && fullThread.AuthorA != AppKeys.keys.xUserId)
                            {

                                // Temos que saber se é thread ou apenas uma chamada simples

                                bool isSingleTweet = true;

                                if (fullThread != null && fullThread.AuthorA != AppKeys.keys.xUserId && fullThread.Tweets.Count > 1)
                                    isSingleTweet = false;

                                // O que o usuário pediu?
                            
                                Console.WriteLine($"Getting command from {tweetId}...");

                                string commandstr = tweet?["text"]?.ToString() ?? "";

                                // Chama o CHAT GPT para identificar o comando

                                OpenAIAPI.IdentifiedCommand cmd = await OpenAIAPI.CheckCommand(commandstr, isSingleTweet);

                                // Checa comando invalido

                                if (cmd == null || cmd.Result == OpenAIAPI.CMD_UNKNOWN || 
                                    (cmd.Result != OpenAIAPI.CMD_HELP && cmd.Result != OpenAIAPI.CMD_SCORE && cmd.Result != OpenAIAPI.CMD_SCOREBOARD && 
                                    cmd.Result != OpenAIAPI.CMD_INVITE && cmd.Result != OpenAIAPI.CMD_ACCEPT_INVITE && cmd.Result != OpenAIAPI.CMD_REFUSE_INVITE && 
                                    cmd.Result!= OpenAIAPI.CMD_THREAD_FALSE && cmd.Result != OpenAIAPI.CMD_THREAD_ARGUE && cmd.Result != OpenAIAPI.CMD_THREAD_WHOISRIGHT))
                                {

                                    // Não entendi o comando

                                    VeraciBot.Data.Tweet failToUnderstandTweet = new Data.Tweet()
                                    {
                                        Id = tweetId,
                                        OriginalText = "",
                                        ThreadId = tweetId,
                                        Text = "",
                                        AuthorId = authorId,
                                        OriginalAuthorId = authorId,
                                        Result = 0
                                    };

                                    dbContext.Tweets.Add(failToUnderstandTweet);
                                    dbContext.SaveChanges();

                                    string failedToUndesrstadText = await Phrases.GetFailedToUnderstandResponseAsync("pt");

                                    await TwitterAPI.PostReplyWithImageAsync(failedToUndesrstadText, Images.failedToUnderstandImage, tweetId);

                                    continue;

                                }

                                // Verficia autorização específica para aceitar (ou não) convite

                                if (authorization != null && authorization.Status == AuthorizedUser.STATUS_INVITED && cmd.Result != OpenAIAPI.CMD_ACCEPT_INVITE && cmd.Result != OpenAIAPI.CMD_REFUSE_INVITE)
                                {

                                    // Não entendi o comando, precisa aceitar ou negar

                                    VeraciBot.Data.Tweet failToUnderstandTweet = new Data.Tweet()
                                    {
                                        Id = tweetId,
                                        OriginalText = "",
                                        ThreadId = tweetId,
                                        Text = "",
                                        AuthorId = authorId,
                                        OriginalAuthorId = authorId,
                                        Result = 0
                                    };

                                    dbContext.Tweets.Add(failToUnderstandTweet);
                                    dbContext.SaveChanges();

                                    string failedToUndesrstadAcceptText = await Phrases.GetFailedToUnderstandAcceptResponseAsync("pt");

                                    await TwitterAPI.PostReplyWithImageAsync(failedToUndesrstadAcceptText, Images.failedToUnderstandAcceptImage, tweetId);

                                    continue;

                                }

                                // Comando de aceitar ou recusar quando autorização já foi feita... não faz sentido... ignorar

                                if (authorization != null && authorization.Status == AuthorizedUser.STATUS_AUTHORIZED && (cmd.Result == OpenAIAPI.CMD_ACCEPT_INVITE || cmd.Result == OpenAIAPI.CMD_REFUSE_INVITE))
                                {
                                    Console.WriteLine($"Tweet {tweetId} aceitação ou recusa já feita. Ignorado.");
                                    continue;
                                }

                                // Executa o comando

                                switch (cmd.Result)
                                {

                                    case OpenAIAPI.CMD_HELP: // Ajuda

                                        VeraciBot.Data.Tweet helpTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(helpTweet);
                                        dbContext.SaveChanges();

                                        string helpResponseText = await Phrases.GetHelpResponseAsync("pt");

                                        await TwitterAPI.PostReplyWithImageAsync(helpResponseText, Images.helpImage, tweetId);
                                        break;

                                    case OpenAIAPI.CMD_SCORE: // Pontuacao

                                        VeraciBot.Data.Tweet scoreTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(scoreTweet);
                                        dbContext.SaveChanges();

                                        TwitterAPI.TwitterUser author = await TwitterAPI.GetTwitterUserById(authorId);
                                        TweetAuthor authorTweet = await TweetAuthor.GetTweetAuthor(dbContext, authorId, author.Username, author.Name);

                                        string finalResponse = await Phrases.GetScoreResponseAsync("pt");
                                        finalResponse += "\r\n\r\n" + authorTweet.GetDescription();

                                        await TwitterAPI.PostReplyWithImageAsync(finalResponse, Images.scoreImage, tweetId);
                                        break;

                                    case OpenAIAPI.CMD_SCOREBOARD: // Taebela de pontuação

                                        VeraciBot.Data.Tweet scoreBoardTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(scoreBoardTweet);
                                        dbContext.SaveChanges();

                                        string boardResponse = await Phrases.GetScoreResponseAsync("pt");
                                        boardResponse += "\r\n\r\n" + TweetAuthor.GetFullScoreBoard(10);

                                        await TwitterAPI.PostReplyWithImageAsync(boardResponse, Images.scoreBoardImage, tweetId);
                                        break;


                                    case OpenAIAPI.CMD_INVITE: // Convidar outra pessoa

                                        VeraciBot.Data.Tweet InviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,                                            
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(InviteTweet);
                                        dbContext.SaveChanges();

                                        string inviteText = await Phrases.GetInviteResponseAsync("pt");

                                        // Identifica outros usuários no tweet

                                        string[] userNames = TwitterAPI.FindUsersReference(commandstr);
                                        string inviteUserName = "";

                                        // Pega o último usuário que não seja o veracibot nem que postou

                                        string authorUserName = (await TwitterAPI.GetTwitterUserById(authorId)).Username.ToLower();

                                        for (int i = userNames.Length - 1; i >= 0; i--)
                                        {
                                            if (userNames[i].ToLower() != "veracibot" && userNames[i].ToLower() != authorUserName)
                                            {
                                                inviteUserName = userNames[i];
                                                break;
                                            }
                                        }

                                        if (inviteUserName != null && inviteUserName != "")
                                        {

                                            // Identifica outros usuários no tweet

                                            TwitterUser userInvite = await TwitterAPI.GetTwitterUserByUserName(inviteUserName);

                                            // Usuário já foi convidado?

                                            var inviteAuthorization = await dbContext.AuthorizedUsers.FirstOrDefaultAsync(e => e.Id == userInvite.Id);
                                            if (inviteAuthorization != null && (inviteAuthorization.Status == AuthorizedUser.STATUS_AUTHORIZED || inviteAuthorization.Status == AuthorizedUser.STATUS_INVITED))
                                            {

                                                // Já foi convidado ou já está jogando
                                                
                                                VeraciBot.Data.Tweet inviteErrorTweet = new Data.Tweet()
                                                {
                                                    Id = tweetId,
                                                    OriginalText = "",
                                                    ThreadId = fullThread.Id,
                                                    Text = "",
                                                    AuthorId = authorId,
                                                    OriginalAuthorId = fullThread.AuthorA,
                                                    Result = 0
                                                };
                                                
                                                dbContext.Tweets.Add(inviteErrorTweet);
                                                dbContext.SaveChanges();
                                                
                                                string inviteErrorText = await Phrases.GetInviteErrorResponseAsync("pt");

                                                await TwitterAPI.PostReplyWithImageAsync(inviteErrorText, Images.inviteErrorImage, tweetId);
                                                
                                                break;

                                            }

                                            if (inviteAuthorization != null && inviteAuthorization.Status == AuthorizedUser.STATUS_NOT_AUTHORIZED)
                                            {

                                                // Se o cara não estava autorizado, pode ser convidado de novo... Atualiza autorização para convidado

                                                inviteAuthorization.AuthorizedById = fullThread.AuthorA;
                                                inviteAuthorization.AuthorizationDate = DateTime.UtcNow;
                                                inviteAuthorization.Status = AuthorizedUser.STATUS_INVITED;

                                                dbContext.AuthorizedUsers.Update(inviteAuthorization);
                                                dbContext.SaveChanges();

                                            }
                                            else
                                            {

                                                // Cria autorização temporária para o usuário convidado

                                                VeraciBot.Data.AuthorizedUser newTempAuth = new Data.AuthorizedUser()
                                                {
                                                    Id = userInvite.Id,
                                                    AuthorizedById = fullThread.AuthorA,
                                                    AuthorizationDate = DateTime.UtcNow,
                                                    Status = AuthorizedUser.STATUS_INVITED
                                                };

                                                dbContext.AuthorizedUsers.Add(newTempAuth);
                                                dbContext.SaveChanges();

                                            }

                                            // Personaliza o convite com o nome do usuário

                                            inviteText = inviteUserName + " " + inviteText;

                                            await TwitterAPI.PostReplyWithImageAsync(inviteText, Images.inviteImage, tweetId);

                                        }
                                        else
                                        {

                                            // Não marcou outro usuário. Burro!

                                            VeraciBot.Data.Tweet inviteNoUserTweet = new Data.Tweet()
                                            {
                                                Id = tweetId,
                                                OriginalText = "",
                                                ThreadId = fullThread.Id,
                                                Text = "",
                                                AuthorId = authorId,
                                                OriginalAuthorId = fullThread.AuthorA,
                                                Result = 0
                                            };

                                            dbContext.Tweets.Add(inviteNoUserTweet);
                                            dbContext.SaveChanges();

                                            string inviteNoUserText = await Phrases.GetInviteNoUserResponseAsync("pt");

                                            await TwitterAPI.PostReplyWithImageAsync(inviteNoUserText, Images.inviteNoUserImage, tweetId);

                                        }
                                        break;

                                    case OpenAIAPI.CMD_ACCEPT_INVITE: // Aceitar convite

                                        VeraciBot.Data.Tweet AcceptInviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(AcceptInviteTweet);
                                        dbContext.SaveChanges();

                                        // Atualiza autorização para definitiva

                                        if (authorization != null)
                                        {

                                            authorization.Status = AuthorizedUser.STATUS_AUTHORIZED;

                                            dbContext.AuthorizedUsers.Update(authorization);
                                            dbContext.SaveChanges();
                                        
                                        }

                                        // Coloca resposta

                                        string acceptText = await Phrases.GetAcceptResponseAsync("pt");

                                        await TwitterAPI.PostReplyWithImageAsync(acceptText, Images.acceptImage, tweetId);

                                        break;

                                    case OpenAIAPI.CMD_REFUSE_INVITE: // Não Aceitar convite

                                        VeraciBot.Data.Tweet RefuseInviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(RefuseInviteTweet);
                                        dbContext.SaveChanges();

                                        // Atualiza autorização para definitiva

                                        if (authorization != null)
                                        {

                                            authorization.Status = AuthorizedUser.STATUS_NOT_AUTHORIZED;

                                            dbContext.AuthorizedUsers.Update(authorization);
                                            dbContext.SaveChanges();

                                        }

                                        // Coloca resposta

                                        string noAcceptText = await Phrases.GetRefuseResponseAsync("pt");

                                        await TwitterAPI.PostReplyWithImageAsync(noAcceptText, Images.refuseImage, tweetId);

                                        break;

                                }

                                // Se for tweet simples, não faz mais nada. O resto só se aplica a threads

                                if (isSingleTweet)
                                    continue;

                                // Já tratou essa thread?

                                var previousThread = await dbContext.Tweets.FirstOrDefaultAsync(e => e.ThreadId == fullThread.Id);
                                if (previousThread != null)
                                {
                                    Console.WriteLine($"Thread {tweetId} already processed.");
                                    continue;
                                }

                                // Checa se tem crédito

                                TwitterAPI.TwitterUser userAuthorA = await TwitterAPI.GetTwitterUserById(fullThread.AuthorA);
                                TwitterAPI.TwitterUser userAuthorB = await TwitterAPI.GetTwitterUserById(fullThread.AuthorB);

                                TweetAuthor authorA = await TweetAuthor.GetTweetAuthor(dbContext, fullThread.AuthorA, userAuthorA.Username, userAuthorA.Name);
                                TweetAuthor authorB = await TweetAuthor.GetTweetAuthor(dbContext, fullThread.AuthorB, userAuthorB.Username, userAuthorB.Name);

                                fullThread.AuthorA = authorA.UserName;
                                fullThread.AuthorB = authorB.UserName;

                                // Executa o comando

                                switch (cmd.Result)
                                {

                                    case OpenAIAPI.CMD_THREAD_FALSE: // Contesta informação da thread
                                        break;

                                    case OpenAIAPI.CMD_THREAD_WHOISRIGHT: // Quem está certo na thread
                                        break;

                                    case OpenAIAPI.CMD_THREAD_ARGUE: // Argumente sobre a thread

                                        // Chama o CHAT GPT

                                        OpenAIAPI.FullEvaluation result = await OpenAIAPI.CheckThread(fullThread);
                                        if (result == null)
                                        {
                                            Console.WriteLine($"Thread {fullThread.Id} failed to check.");
                                            continue;
                                        }

                                        // Prepara a resposta

                                        VeraciBot.Data.Tweet fullResponseTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            ThreadId = fullThread.Id,
                                            Text = fullThread.GetStartB(),
                                            OriginalText = fullThread.GetStartA(),
                                            AuthorId = fullThread.AuthorB,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Date = DateTime.UtcNow,
                                            Result = result.Result
                                        };

                                        fullResponseTweet.ComputeAuthors(dbContext).Wait();

                                        dbContext.Tweets.Add(fullResponseTweet);
                                        dbContext.SaveChanges();

                                        string fullResponseImage = "img/resp" + result.Result + ".jpg";
                                        string fullResponseText = result.Response;

                                        fullResponseText = "@" + authorA.UserName + ": " + fullResponseText + "\n\n" + authorA.GetDescription() + "\n" + authorB.GetDescription();

                                        await TwitterAPI.PostReplyWithImageAsync(fullResponseText, fullResponseImage, tweetId);

                                        break;

                                }

                            }

                        }

                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());

                }

                Thread.Sleep(60000);

            }

        }

    }

}