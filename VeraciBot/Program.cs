using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.Data;


namespace VeraciBot
{

    class Program
    {

        public static string notAuthorizedImage = "img/no.jpg";
        public static string notAuthorizedResponse = "You are not authorized to use VERACIBOT. You must be invited to play this game.";

        public static string[] helpImage = { "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg" };
        public static string[] helpResponse = {
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. For more details about the project, visit https://veraci.bot.",
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. For more details about the project, visit https://veraci.bot.",
            "Esse é o VERACIBOT, seu robô para verificação de fatos aprovado pelo Ministério da Verdade. Para saber mais detalhes sobre o projeto consulte https://veraci.bot.",
            "Este es VERACIBOT, su robot de verificación de hechos aprobado por el Ministerio de la Verdad. Para más detalles sobre el proyecto, visite https://veraci.bot.",
            "Dies ist VERACIBOT, Ihr Faktenprüfungsroboter, der vom Ministerium für Wahrheit genehmigt wurde. Für weitere Details zum Projekt besuchen Sie https://veraci.bot.",
            "C'est VERACIBOT, votre robot de vérification des faits approuvé par le Ministère de la Vérité. Pour plus de détails sur le projet, visitez https://veraci.bot.",
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. For more details about the project, visit https://veraci.bot.",
        };

        public static string[] pontImage = { "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg" };
        public static string[] pontResponse = {
            "Your score is",
            "Your score is",
            "Sua pontuação é",
            "Tu puntuación es",
            "Ihre Punktzahl beträgt",
            "Votre score est",
            "Your score is",
        };

        static string[] resp = {
            "Vá imediatamente para a cadeia, seu bolsonarista fazedor de fakenews. 17 anos de cadeia imediatamente!",
            "Procure a carmen lucia para iniciar o curso de democracia relativa do tse e se prepara pra visita do Uber Black da PF",
            "Ok, vou deixar passar essa só com 14 anos de cadeia, na próxima vai ser punido de verdade",
            "Parabens por ser um gado muito obediente e só falar a verdade aprovada pelo sistema",
            "Ahhrá... agora sim, tudo certo... ganhou TROFEU DEMOCRACIA RELATIVA do XANDÃO"
        };

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

                            string tweetId = tweet["id"].ToString();
                            string authorId = tweet["author_id"].ToString();

                            if (authorId == AppKeys.keys.xUserId)
                            {
                                Console.WriteLine($"Tweet {tweetId} is from the bot itself.");
                                continue;
                            }

                            string tweetDate = tweet["created_at"].ToString();

                            lastTime = DateTime.Parse(tweetDate);
                            DbConfig.SetLastDateTimeForTwitterCheck(dbContext, lastTime).Wait();

                            var authorization = await dbContext.AuthorizedUsers.FirstOrDefaultAsync(e => e.Id == authorId);
                            if (authorization == null)
                            {

                                VeraciBot.Data.Tweet helpTweet = new Data.Tweet()
                                {
                                    Id = tweetId,
                                    OriginalText = "",
                                    Text = "",
                                    AuthorId = authorId,
                                    Result = 0
                                };

                                dbContext.Tweets.Add(helpTweet);
                                dbContext.SaveChanges();

                                await TwitterAPI.PostReplyWithImageAsync(notAuthorizedResponse, notAuthorizedImage, tweetId);

                                continue;

                            }

                            var previousTweet = await dbContext.Tweets.FirstOrDefaultAsync(e => e.Id == tweetId);
                            if (previousTweet != null)
                            {
                                Console.WriteLine($"Tweet {tweetId} already processed.");
                                continue;
                            }

                            Console.WriteLine($"Getting full thread {tweetId}...");

                            TwitterAPI.ThreadContext fullThread = await TwitterAPI.GetThreadContext(tweetId, authorId);
                            if (fullThread != null && fullThread.AuthorA != AppKeys.keys.xUserId)
                            {

                                // Se for só um tweet, não precisa de thread, só fale a resposta    

                                if (fullThread.Tweets.Count == 1)
                                {

                                    Console.WriteLine($"Tweet {tweetId} is a single tweet.");

                                    // Qual o tweet?

                                    string commandstr = fullThread.Tweets[0].Text;

                                    if (commandstr == "")
                                    {

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

                                        await TwitterAPI.PostReplyWithImageAsync(helpResponse[0], helpImage[0], tweetId);

                                        continue;

                                    }
                                    else
                                    {

                                        // Chama o CHAT GPT

                                        OpenAIAPI.IdentifiedCommand cmd = await OpenAIAPI.CheckCommand(commandstr);
                                        if (cmd == null)
                                        {
                                            Console.WriteLine($"Thread {fullThread.Id} failed to check.");
                                            continue;
                                        }


                                        switch (cmd.Result)
                                        {
                                            case 1: // Ajuda

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

                                                await TwitterAPI.PostReplyWithImageAsync(helpResponse[cmd.Language], helpImage[cmd.Language], tweetId);
                                                break;

                                            case 2: // Pontuacao

                                                VeraciBot.Data.Tweet pontTweet = new Data.Tweet()
                                                {
                                                    Id = tweetId,
                                                    OriginalText = "",
                                                    ThreadId = fullThread.Id,
                                                    Text = "",
                                                    AuthorId = authorId,
                                                    OriginalAuthorId = fullThread.AuthorA,
                                                    Result = 0
                                                };

                                                dbContext.Tweets.Add(pontTweet);
                                                dbContext.SaveChanges();

                                                TwitterAPI.TwitterUser author = await TwitterAPI.GetTwitterUserById(authorId);
                                                TweetAuthor authorTweet = await TweetAuthor.GetTweetAuthor(dbContext, authorId, author.Username, author.Name);

                                                string finalResponse = pontResponse[cmd.Language] + ": " + authorTweet.GetDescription();

                                                await TwitterAPI.PostReplyWithImageAsync(finalResponse, pontImage[cmd.Language], tweetId);
                                                break;


                                        }

                                        continue;

                                    }

                                }

                                // Não responder a mesma thread

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

                                if (authorB.Value <= 0)
                                {

                                    Console.WriteLine($"Author {authorB.UserName} do not have credit.");

                                    VeraciBot.Data.Tweet notTweet = new Data.Tweet()
                                    {
                                        Id = tweetId,
                                        OriginalText = "",
                                        ThreadId = fullThread.Id,
                                        Text = "",
                                        AuthorId = authorId,
                                        OriginalAuthorId = fullThread.AuthorA,
                                        Result = 0
                                    };

                                    dbContext.Tweets.Add(notTweet);
                                    dbContext.SaveChanges();

                                    string notImgem = "img/nao.jpg";
                                    string notResponse = "Você não tem crédito para usar o VERACIBOT, precisa se comportar melhor! sinto muito!";

                                    notResponse = notResponse + "\n\n" + authorB.GetDescription();

                                    await TwitterAPI.PostReplyWithImageAsync(notResponse, notImgem, tweetId);

                                    continue;

                                }

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