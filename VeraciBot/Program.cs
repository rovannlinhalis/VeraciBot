using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using VeraciBot.Data;
using static VeraciBot.TwitterAPI;


namespace VeraciBot
{

    class Program
    {

        public static string notAuthorizedImage = "img/no.jpg";
        public static string notAuthorizedResponse = "You are not authorized to use VERACIBOT. You must be invited to play this game.";

        public static string[] helpImage = { "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg" };
        public static string[] helpResponse = {
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. With this bot, you can expose other petty tyrants who attack our supreme democracy with fake news. This might help you get a lighter sentence or screw over other people for free. For more details about the project, visit https://veraci.bot.",
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. With this bot, you can expose other petty tyrants who attack our supreme democracy with fake news. This might help you get a lighter sentence or screw over other people for free. For more details about the project, visit https://veraci.bot.",
            "Esse é o VERACIBOT, seu robô para verificação de fatos aprovado pelo Ministério da Verdade. Com o uso desse robô, você pode dedurar outros pequenos tiraninhos que ficam atacando nossa suprema democracia com fake-news. Com isso, talvez, conseguir uma dosimetria de pena para você ou ferrar outras pessoas de graça. Para saber mais detalhes sobre o projeto consulte https://veraci.bot.",
            "Este es VERACIBOT, su robot de verificación de hechos aprobado por el Ministerio de la Verdad. Con este bot, puedes exponer a otros tiranos que atacan nuestra suprema democracia con noticias falsas. Esto podría ayudarte a obtener una sentencia más leve o a perjudicar a otros sin costo alguno. Para más detalles sobre el proyecto, visite https://veraci.bot.",
            "Dies ist VERACIBOT, Ihr Faktenprüfungsroboter, der vom Ministerium für Wahrheit genehmigt wurde. Mit diesem Bot kannst du andere Kleinganoven entlarven, die unsere Demokratie mit Fake News angreifen. Das könnte dir zu einer milderen Strafe verhelfen oder dir ermöglichen, andere kostenlos zu schädigen. Für weitere Details zum Projekt besuchen Sie https://veraci.bot.",
            "C'est VERACIBOT, votre robot de vérification des faits approuvé par le Ministère de la Vérité. Ce bot vous permettra de démasquer les petits tyrans qui s'attaquent à notre démocratie en diffusant de fausses informations. Vous pourriez ainsi obtenir une peine plus légère ou nuire gratuitement à d'autres. Pour plus de détails sur le projet, visitez https://veraci.bot.",
            "This is VERACIBOT, your fact-checking robot approved by the Ministry of Truth. With this bot, you can expose other petty tyrants who attack our supreme democracy with fake news. This might help you get a lighter sentence or screw over other people for free. For more details about the project, visit https://veraci.bot.",
        };

        public static string[] pontImage = { "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg" };
        public static string[] pontResponse = {
            "How dare you bother me with your petty demands, you little tyrant! This is an attack on our democracy! Your trial is confidential... but a journalist leaked some information:",
            "How dare you bother me with your petty demands, you little tyrant! This is an attack on our democracy! Your trial is confidential... but a journalist leaked some information:",
            "Como ousa me incomodar com seus pedidos insignificantes, seu tiraninho! Isso é um ataque a nossa democracia! Seu processo é sigiloso... mas uma jornalista vazou umas informações:",
            "¡Cómo te atreves a molestarme con tus mezquinas exigencias, pequeño tirano! ¡Esto es un atentado contra nuestra democracia! Tu juicio es confidencial... pero un periodista filtró información:",
            "Wie kannst du es wagen, mich mit deinen kleinlichen Forderungen zu belästigen, du kleiner Tyrann! Das ist ein Angriff auf unsere Demokratie! Dein Prozess ist vertraulich... aber ein Journalist hat einige Informationen durchsickern lassen:",
            "Comment osez-vous me harceler avec vos exigences mesquines, petit tyran ! C'est une attaque contre notre démocratie ! Votre procès est confidentiel… mais un journaliste a divulgué quelques informations:",
            "How dare you bother me with your petty demands, you little tyrant! This is an attack on our democracy! Your trial is confidential... but a journalist leaked some information:",
        };

        public static string[] scoreImage = { "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg", "img/logo.jpg" };
        public static string[] scoreResponse = {
            "The list of the biggest petty tyrants shows the degradation of our supreme democracy! We need to arrest more people to civilize this nation! See the worst:",
            "The list of the biggest petty tyrants shows the degradation of our supreme democracy! We need to arrest more people to civilize this nation! See the worst:",
            "A tabela dos maiores pequenos tiranos mostra a degradação da nossa suprema democracia! É preciso prender mais gente para civilizar esse povo! Veja os piores:",
            "¡La lista de los mayores tiranos mezquinos muestra la degradación de nuestra suprema democracia! ¡Necesitamos arrestar a más gente para civilizar esta nación! Vean lo peor:",
            "Die Liste der größten Kleintyrannen zeigt den Verfall unserer angeblich so hochgehaltenen Demokratie! Wir müssen mehr Menschen verhaften, um dieses Land wieder zu zivilisieren! Sehen Sie die Schlimmsten:",
            "La liste des plus grands petits tyrans témoigne de la dégradation de notre démocratie ! Il faut arrêter davantage de personnes pour civiliser ce pays ! Voyez les pires exemples:",
            "The list of the biggest petty tyrants shows the degradation of our supreme democracy! We need to arrest more people to civilize this nation! See the worst:",
        };

        public static string[] inviteImage = { "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg" };
        public static string[] inviteResponse = {
            "You have been identified as one of the millions of petty tyrants who have a mania for attacking supreme democracy with fake news. Your sentence is 17 years in prison without amnesty. But we offer the possibility of a plea bargain. Do you accept to participate in this game? Learn how VERACIBOT works at https://veraci.bot.",
            "You have been identified as one of the millions of petty tyrants who have a mania for attacking supreme democracy with fake news. Your sentence is 17 years in prison without amnesty. But we offer the possibility of a plea bargain. Do you accept to participate in this game? Learn how VERACIBOT works at https://veraci.bot.",
            "Você foi identificado como um dos milhões de tiraninhos que tem mania de agredir a democracia suprema com fake news. Sua pena é de 17 anos de cadeia sem anistia. Mas oferecemos a possibilidade de delação premiada. Você aceita participar desse jogo? Saiba como funciona o VERACIBOT em https://veraci.bot.",
            "Se le ha identificado como uno de los millones de tiranos mezquinos obsesionados con atacar la suprema democracia con noticias falsas. Su condena es de 17 años de prisión sin amnistía. Pero le ofrecemos la posibilidad de un acuerdo con la fiscalía. ¿Acepta participar en este juego? Descubra cómo funciona VERACIBOT en https://veraci.bot.",
            "Sie wurden als einer der Millionen von Kleintyrannen identifiziert, die mit einer Manie die Demokratie mit Falschnachrichten angreifen. Ihre Strafe beträgt 17 Jahre Haft ohne Begnadigung. Wir bieten Ihnen jedoch die Möglichkeit einer Verständigung an. Sind Sie bereit, an diesem Spiel teilzunehmen? Erfahren Sie mehr über die Funktionsweise von VERACIBOT unter https://veraci.bot.",
            "Vous avez été identifié comme l'un des millions de petits tyrans qui s'adonnent à la désinformation en attaquant la démocratie. Votre peine est de 17 ans de prison sans possibilité de libération conditionnelle. Cependant, nous vous proposons une négociation de peine. Acceptez-vous de participer à ce jeu ? Découvrez le fonctionnement de VERACIBOT sur https://veraci.bot.",
            "You have been identified as one of the millions of petty tyrants who have a mania for attacking supreme democracy with fake news. Your sentence is 17 years in prison without amnesty. But we offer the possibility of a plea bargain. Do you accept to participate in this game? Learn how VERACIBOT works at https://veraci.bot.",
        };

        public static string[] acceptImage = { "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg" };
        public static string[] acceptResponse = {
            "By accepting this invitation, you confirm that you are indeed a petty tyrant who attacks our supreme democracy with fake news. I didn't need proof before, and I need it even less now. Now your job is to snitch on others to try and reduce your sentence. Good luck in the game.",
            "By accepting this invitation, you confirm that you are indeed a petty tyrant who attacks our supreme democracy with fake news. I didn't need proof before, and I need it even less now. Now your job is to snitch on others to try and reduce your sentence. Good luck in the game.",
            "Ao aceitar esse convite, você confirma que é mesmo um tiraninho que ataca nossa suprema democracia com fake-news. Eu não precisava de provas antes e preciso menos ainda agora. Agora sua função é dedurar os outros para tentar reduzir sua pena. Boa sorte no jogo.",
            "Al aceptar esta invitación, confirmas que eres un tirano de poca monta que ataca nuestra suprema democracia con noticias falsas. Antes no necesitaba pruebas, y ahora las necesito aún menos. Ahora tu trabajo es delatar a otros para intentar reducir tu condena. ¡Buena suerte en el juego!",
            "Mit der Annahme dieser Einladung bestätigen Sie, dass Sie tatsächlich ein kleinlicher Tyrann sind, der unsere Demokratie mit Falschnachrichten angreift. Ich brauchte vorher keine Beweise, und jetzt brauche ich sie erst recht nicht. Nun ist es Ihre Aufgabe, andere zu denunzieren, um Ihre Strafe zu mildern. Viel Glück dabei.",
            "En acceptant cette invitation, vous confirmez être un petit tyran qui s'attaque à notre démocratie suprême avec de fausses informations. Je n'avais pas besoin de preuves auparavant, et j'en ai encore moins besoin maintenant. Désormais, votre rôle est de dénoncer les autres pour tenter d'obtenir une réduction de peine. Bonne chance!",
            "By accepting this invitation, you confirm that you are indeed a petty tyrant who attacks our supreme democracy with fake news. I didn't need proof before, and I need it even less now. Now your job is to snitch on others to try and reduce your sentence. Good luck in the game.",
        };

        public static string[] noAcceptImage = { "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg", "img/invite.jpg" };
        public static string[] noAcceptResponse = {
            "Don't think you'll escape my pursuit just by refusing to participate in our game. Goodbye. For now.",
            "Don't think you'll escape my pursuit just by refusing to participate in our game. Goodbye. For now.",
            "Não ache que você vai escapar da minha perseguição apenas por negar participar no nosso jogo. Tchau. Por equanto.",
            "No creas que escaparás de mi persecución simplemente negándote a participar en nuestro juego. Adiós. Por ahora.",
            "Glaub ja nicht, dass du meiner Verfolgung entkommst, indem du dich weigerst, an unserem Spiel teilzunehmen. Auf Wiedersehen. Vorläufig.",
            "Ne crois pas que tu échapperas à mes poursuites simplement en refusant de participer à notre jeu. Au revoir. Pour l'instant.",
            "Don't think you'll escape my pursuit just by refusing to participate in our game. Goodbye. For now.",
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

                                string notAuthorizedResponseText = await OpenAIAPI.VariatePhrase(notAuthorizedResponse);

                                await TwitterAPI.PostReplyWithImageAsync(notAuthorizedResponseText, notAuthorizedImage, tweetId);

                                continue;

                            }

                            var previousTweet = await dbContext.Tweets.FirstOrDefaultAsync(e => e.Id == tweetId);
                            if (previousTweet != null)
                            {
                                Console.WriteLine($"Tweet {tweetId} already processed.");
                                continue;
                            }

                            Console.WriteLine($"Getting command from {tweetId}...");

                            // O que o usuário pediu?
                            string commandstr = tweet?["text"]?.ToString() ?? "";

                            Console.WriteLine($"Getting full thread {tweetId}...");

                            // Temos que saber se é thread ou apenas uma chamada simples
                            TwitterAPI.ThreadContext fullThread = await TwitterAPI.GetThreadContext(tweetId, authorId);

                            bool isSingleTweet = true;

                            if (fullThread != null && fullThread.AuthorA != AppKeys.keys.xUserId && fullThread.Tweets.Count > 1)
                                isSingleTweet = false;

                            // Chama o CHAT GPT

                            OpenAIAPI.IdentifiedCommand cmd = await OpenAIAPI.CheckCommand(commandstr, isSingleTweet);
                            if (cmd == null)
                            {
                                Console.WriteLine($"Command {tweetId} failed to get command.");
                                continue;
                            }

                            if (fullThread != null && fullThread.AuthorA != AppKeys.keys.xUserId)
                            {

                                // Se for só um tweet, não precisa de thread, só fale a resposta    

                                if (fullThread.Tweets.Count == 1)
                                {

                                    Console.WriteLine($"Tweet {tweetId} is a single tweet.");

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

                                        string helpResponseText = await OpenAIAPI.VariatePhrase(helpResponse[cmd.Language]);

                                        await TwitterAPI.PostReplyWithImageAsync(helpResponseText, helpImage[cmd.Language], tweetId);
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


                                    case 4: // Convidar outra pessoa

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

                                        string inviteText = await OpenAIAPI.VariatePhrase(inviteResponse[cmd.Language]);

                                        // Identifica outros usuários no tweet

                                        string[] userNames = TwitterAPI.FindUsersReference(commandstr);
                                        string inviteUserName = null;

                                        if (userNames.Length > 0)
                                        {
                                            inviteUserName = userNames[userNames.Length - 1];
                                        }

                                        if (inviteUserName != null)
                                        {

                                            // Identifica outros usuários no tweet

                                            TwitterUser userInvite = await TwitterAPI.GetTwitterUserByUserName(inviteUserName);

                                            VeraciBot.Data.AuthorizedUser newTempAuth = new Data.AuthorizedUser()
                                            {
                                                Id = userInvite.Id,
                                                AuthorizedById = fullThread.AuthorA,
                                                AuthorizationDate = DateTime.UtcNow,
                                                Status = 2,
                                            };

                                            dbContext.AuthorizedUsers.Add(newTempAuth);
                                            dbContext.SaveChanges();

                                            // Personaliza o convite com o nome do usuário

                                            inviteText = inviteUserName + " " + inviteText;

                                            await TwitterAPI.PostReplyWithImageAsync(inviteText, helpImage[cmd.Language], tweetId);

                                        }
                                        else
                                        {

                                            // Erro não chamou um usuario


                                        }
                                        break;

                                    case 5: // Aceitar convite

                                        if (authorization.Status == 2)
                                        {

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

                                            string acceptText = await OpenAIAPI.VariatePhrase(acceptResponse[cmd.Language]);

                                            // Identifica outros usuários no tweet


                                            // TEM que alterar a autorização para definitiva
                                            // TEM que checar autoriza'~oes temporarias e definitias

                                            TwitterUser user = await TwitterAPI.GetTwitterUserById(authorId);

                                            VeraciBot.Data.AuthorizedUser newAuth = new Data.AuthorizedUser()
                                            {
                                                Id = authorId,
                                                AuthorizedById = fullThread.AuthorA,
                                                AuthorizationDate = DateTime.UtcNow,
                                                Status = 1,
                                            };

                                            dbContext.AuthorizedUsers.Add(newAuth);
                                            dbContext.SaveChanges();

                                            await TwitterAPI.PostReplyWithImageAsync(acceptText, helpImage[cmd.Language], tweetId);

                                        }
                                        
                                        break;

                                }

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
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());

                }

                Thread.Sleep(60000);

            }

        }

    }

}