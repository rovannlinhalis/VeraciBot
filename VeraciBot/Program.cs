using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.Data;


namespace VeraciBot
{

    class Program
    {

        static string[] resp = {
            "Vá imediatamente para a cadeia, seu bolsonarista fazedor de fakenews. 17 anos de cadeia imediatamente!",
            "Procure a carmen lucia para iniciar o curso de democracia relativa do tse e se prepara pra visita do Uber Black da PF",
            "Ok, vou deixar passar essa só com 14 anos de cadeia, na próxima vai ser punido de verdade",
            "Parabens por ser um gado muito obediente e só falar a verdade aprovada pelo sistema",
            "Ahhrá... agora sim, tudo certo... ganhou TROFEU DEMOCRACIA RELATIVA do XANDÃO"
        };

        private const string USER_ID_PETER_ANCAPSU = "778933271354826752";

        static async Task Main(string[] args)
        {

            Console.WriteLine("Starting VERACIBOT");

            // Cria duas tarefas independentes
            Task tarefa1 = ThreadCicloTwitterChatGpt();
            Task tarefa2 = ThreadCicloPontuacao();

            Console.WriteLine("As tarefas foram iniciadas...");

            // Aguarda as duas tarefas terminarem
            await Task.WhenAll(tarefa1, tarefa2);

            Console.WriteLine("Programa finalizado.");

        }

        static async Task ThreadCicloPontuacao()
        {

            Console.WriteLine("PONT: Connecting VERACIBOT database");

            var services = new ServiceCollection();

            services.AddDbContext<VeraciDbContext>(options => options.UseSqlServer(AppKeys.keys.dbConnection));
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<VeraciDbContext>();

            // Cria o banco e a tabela automaticamente se não existirem
            dbContext.Database.EnsureCreated();

            Console.WriteLine("PONT: Starting calculo de pontos");

            List<VeraciBot.Data.TweetAuthor> tweetAuthors = dbContext.TweetAuthors.Where(p => p.UserName == "").ToList();

            foreach (var authors in tweetAuthors)
            {

                try
                {

                    if (authors.UserName == "")
                    {
                        string name = await TwitterAPI.GetUsernameById(authors.Id);
                        if (name != null)
                        {
                            authors.UserName = name;
                            dbContext.TweetAuthors.Update(authors);
                            dbContext.SaveChanges();
                        }
                    }   

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

            }

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
                            string tweetAuthorId = tweet["author_id"].ToString();
                            string tweetText = tweet["text"].ToString();
                            string tweetDate = tweet["created_at"].ToString();

                            lastTime = DateTime.Parse(tweetDate);

                            tweetText = TwitterAPI.RemoverReferencias(tweetText); // Remove referências de @   

                            if (tweetAuthorId != USER_ID_PETER_ANCAPSU)
                            {
                                Console.WriteLine($"Tweet {tweetId} not authorized.");
                                continue;

                            }

                            var tweet_previo = await dbContext.Tweets.FirstOrDefaultAsync(e => e.Id == tweetId);
                            if (tweet_previo != null)
                            {
                                Console.WriteLine($"Tweet {tweetId} already processed.");
                                continue;
                            }

                            Console.WriteLine($"Responding tweet {tweetId}...");

                            var original = await TwitterAPI.GetRepliedTweetTextAndCheck(tweetId);
                            if (original != null && original.AuthorId != AppKeys.keys.xUserId)
                            {

                                string afirmacao = original.Text;

                                int result = 0;

                                if (tweetText != "")
                                {

                                    result = await OpenAIAPI.AvaliarArgumentoAsync(afirmacao, tweetText);
                                    if (result != null)
                                    {

                                        string imgem = "img/resp" + result + ".jpg";
                                        string resposta = resp[result - 1];

                                        await TwitterAPI.PostReplyWithImageAsync(resposta, imgem, tweetId);

                                    }

                                }
                                else
                                {

                                    result = await OpenAIAPI.AvaliarVeracidadeAsync(afirmacao);
                                    if (result != null)
                                    {

                                        string imgem = "img/resp" + result + ".jpg";
                                        string resposta = resp[result - 1];

                                        await TwitterAPI.PostReplyWithImageAsync(resposta, imgem, tweetId);

                                    }

                                }

                                VeraciBot.Data.Tweet internat_tweet = new Data.Tweet()
                                {
                                    Id = tweetId,
                                    OriginalText = original.Text,
                                    ThreadId = original.TweetId,
                                    Text = tweetText == ""? "Is false": tweetText,
                                    AuthorId = tweetAuthorId,
                                    OriginalAuthorId = original.AuthorId,   
                                    Result = result
                                };

                                internat_tweet.ComputeAuthors(dbContext).Wait();

                                dbContext.Tweets.Add(internat_tweet);
                                dbContext.SaveChanges();

                            }

                        }

                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());

                }

                //lastCheck.Value = lastTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                //dbContext.Configs.Update(lastCheck);
                //dbContext.SaveChanges();

                Thread.Sleep(60000);

            }

        }


    }

}