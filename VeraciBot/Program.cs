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
        public static string notAuthorizedResponse = "Quem é você? Você não é ninguém! Não está autorizado a me chamar. Volte quando estiver nos inquéritos do fim do mundo. Aqui só com convite! Para mais detahes, veja https://veraci.bot.";

        public static string failedToUnderstandImage = "img/duvida.jpg";
        public static string failedToUnderstandResponse = "Não entedi o que você quer. Tome cuidado que qualquer erro são mais 17 anos de cadeia. Se quiser ajuda, peça ajuda. Para mais detahes, veja https://veraci.bot.";
        
        public static string failedToUnderstandAcceptImage = "img/duvida.jpg";
        public static string failedToUnderstandAcceptResponse = "Não entedi o que você quer. Você tem que aceitar ou recusar o convite antes de mais nada. Se quiser ajuda, peça ajuda. Para mais detahes, veja https://veraci.bot.";

        public static string helpImage = "img/logo.jpg";
        public static string helpResponse = "Esse é o VERACIBOT, seu robô para verificação de fatos aprovado pelo Ministério da Verdade. Com o uso desse robô, você pode dedurar outros pequenos tiraninhos que ficam atacando nossa suprema democracia com fake-news. Com isso, talvez, conseguir uma dosimetria de pena para você ou ferrar outras pessoas de graça. Para saber mais detalhes, veja https://veraci.bot.";
            
        public static string pontImage = "img/logo.jpg";
        public static string pontResponse = "Como ousa me incomodar com seus pedidos insignificantes, seu tiraninho! Isso é um ataque a nossa democracia! Seu processo é sigiloso... mas uma jornalista vazou umas informações:";

        public static string scoreImage = "img/logo.jpg";
        public static string scoreResponse = "A tabela dos maiores pequenos tiranos mostra a degradação da nossa suprema democracia! É preciso prender mais gente para civilizar esse povo! Veja os piores:";

        public static string inviteImage = "img/invite.jpg";
        public static string inviteResponse = "Você foi identificado como um dos milhões de tiraninhos que tem mania de agredir a democracia suprema com fake news. Sua pena é de 17 anos de cadeia sem anistia. Mas oferecemos a possibilidade de delação premiada. Você aceita participar desse jogo? Saiba como funciona o VERACIBOT em https://veraci.bot.";

        public static string inviteErrorImage = "img/no.jpg";
        public static string inviteErrorResponse = "Esse tiraninho já foi convidado para o inquérito do fim do mundo! Ou já está jogando o jogo ou estamos lidando com ele de outra forma. Escolha outra vítima para suas ambições maquiavélicas!";

        public static string inviteNoUserImage = "img/no.jpg";
        public static string inviteNoUserResponse = "Para convidar alguém, você precisa marcar o usuário que quer convidar, além do VERACIBOT. Não abuse da minha paciência suprema. Qualquer coisa é mais 14 anos de cadeia.";

        public static string acceptImage = "img/logo.jpg";
        public static string acceptResponse = "Ao aceitar esse convite, você confirma que é mesmo um tiraninho que ataca nossa suprema democracia com fake-news. Eu não precisava de provas antes e preciso menos ainda agora. Agora sua função é dedurar os outros para tentar reduzir sua pena. Boa sorte no jogo.";

        public static string noAcceptImage = "img/no.jpg";
        public static string noAcceptResponse = "Não ache que você vai escapar da minha perseguição apenas por negar participar no nosso jogo. Seu nome continua sendo discutido em nossos grupos de WhatsApp. Tchau. Por equanto.";

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

                                string notAuthorizedResponseText = await OpenAIAPI.VariatePhrase(notAuthorizedResponse);

                                await TwitterAPI.PostReplyWithImageAsync(notAuthorizedResponseText, notAuthorizedImage, tweetId);

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

                                    string failedToUndesrstadText = await OpenAIAPI.VariatePhrase(failedToUnderstandResponse);

                                    await TwitterAPI.PostReplyWithImageAsync(failedToUndesrstadText, failedToUnderstandImage, tweetId);

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

                                    string failedToUndesrstadAcceptText = await OpenAIAPI.VariatePhrase(failedToUnderstandAcceptResponse);

                                    await TwitterAPI.PostReplyWithImageAsync(failedToUndesrstadAcceptText, failedToUnderstandAcceptImage, tweetId);

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

                                        string helpResponseText = await OpenAIAPI.VariatePhrase(helpResponse);

                                        await TwitterAPI.PostReplyWithImageAsync(helpResponseText, helpImage, tweetId);
                                        break;

                                    case OpenAIAPI.CMD_SCORE: // Pontuacao

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

                                        string finalResponse = pontResponse + ": " + authorTweet.GetDescription();

                                        await TwitterAPI.PostReplyWithImageAsync(finalResponse, pontImage, tweetId);
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

                                        string inviteText = await OpenAIAPI.VariatePhrase(inviteResponse);

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
                                                
                                                string inviteErrorText = await OpenAIAPI.VariatePhrase(inviteErrorResponse);
                                                
                                                await TwitterAPI.PostReplyWithImageAsync(inviteErrorText, inviteErrorImage, tweetId);
                                                
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

                                            await TwitterAPI.PostReplyWithImageAsync(inviteText, inviteImage, tweetId);

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

                                            string inviteNoUserText = await OpenAIAPI.VariatePhrase(inviteNoUserResponse);

                                            await TwitterAPI.PostReplyWithImageAsync(inviteNoUserText, inviteNoUserImage, tweetId);

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

                                        string acceptText = await OpenAIAPI.VariatePhrase(acceptResponse);

                                        await TwitterAPI.PostReplyWithImageAsync(acceptText, acceptImage, tweetId);

                                        break;

                                    case OpenAIAPI.CMD_REFUSE_INVITE: // Não Aceitar convite

                                        VeraciBot.Data.Tweet NoAcceptInviteTweet = new Data.Tweet()
                                        {
                                            Id = tweetId,
                                            OriginalText = "",
                                            ThreadId = fullThread.Id,
                                            Text = "",
                                            AuthorId = authorId,
                                            OriginalAuthorId = fullThread.AuthorA,
                                            Result = 0
                                        };

                                        dbContext.Tweets.Add(NoAcceptInviteTweet);
                                        dbContext.SaveChanges();

                                        // Atualiza autorização para definitiva

                                        if (authorization != null)
                                        {

                                            authorization.Status = AuthorizedUser.STATUS_NOT_AUTHORIZED;

                                            dbContext.AuthorizedUsers.Update(authorization);
                                            dbContext.SaveChanges();

                                        }

                                        // Coloca resposta

                                        string noAcceptText = await OpenAIAPI.VariatePhrase(noAcceptResponse);

                                        await TwitterAPI.PostReplyWithImageAsync(noAcceptText, noAcceptImage, tweetId);

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