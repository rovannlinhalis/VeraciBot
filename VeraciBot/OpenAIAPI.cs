using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure;

namespace VeraciBot
{

    public class OpenAIAPI
    {

        public class IdentifiedCommand
        {

            public int Result { get; set; } = 0; // 0 = Não Identificado, 1 = Auda, 2 = Pontuação, 3 = Scoreboard, etc vide abaixo

            public string Language { get; set; } = "pt";

        }

        // Prompts para identificação de comando

        public static string cmdPromptCmdSingle = "Identifique no prompt o que o usuário quer de resposta do sistema entre " +
                                                    "1='Ajuda, como funciona o sistema', " +
                                                    "2='Ver minha pontuação', " +
                                                    "3='Ver o quadro de líderes', " +
                                                    "10='Quero convidar outra pessoa', " +
                                                    "20='Aceito o convite' " +
                                                    "21='Não aceito o convite' ";

        public static string cmdPromptCmdThread = "30='Acho que esse conteudo é falso', " +
                                                    "31='Estou argumentando algo diferente ao conteúdo anterior', " +
                                                    "32='Quem está certo nessa discussão' ";

        public const int CMD_UNKNOWN = 0;
        public const int CMD_HELP = 1;
        public const int CMD_SCORE = 2;
        public const int CMD_SCOREBOARD = 3;       
        public const int CMD_INVITE = 10;
        public const int CMD_ACCEPT_INVITE = 20;
        public const int CMD_REFUSE_INVITE = 21;
        public const int CMD_THREAD_FALSE = 30;
        public const int CMD_THREAD_ARGUE = 31;
        public const int CMD_THREAD_WHOISRIGHT = 32;

        public static string cmdPromptCmdEnd = "ou então 0 se o comando não identificado ou qualquer outro comando.";

        public static string cmdPromptLanguage = "Você deve identificar qual a lingua usada pelo usuário. ";

        public static string cmdPromptCmdFinal = "Responda com o número do comando, seguido da lingua, separados por vírgula.";

        /// <summary>
        /// Identifica o comando do usuário
        /// </summary>
        /// <param name="commandstr">Comando explícito do usuário</param>
        /// <param name="issingle">tweet simples ou thread</param>
        /// <returns></returns>
        public static async Task<IdentifiedCommand> CheckCommand(string commandstr, bool issingle)
        {

            IdentifiedCommand resp = new IdentifiedCommand() { Language = "pt", Result = 0 };

            // Se comando é vazio, só tem duas possibilidades, não precisa perguntar pro chatgpt

            if (commandstr == null || commandstr =="" || commandstr.Length < 5)
            {

                if(issingle)
                    resp.Result = CMD_HELP;
                else
                    resp.Result = CMD_THREAD_FALSE;

                resp.Language = ""; // Não tem como saber a lingua (pegar pelo usuário) 

                return resp;

            }

            // Chama OpenAI para identificar o comando

            try
            {

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                        {
                        new { role = "system", content = cmdPromptLanguage + cmdPromptCmdSingle + (issingle? "": cmdPromptCmdThread) +cmdPromptCmdEnd + cmdPromptCmdFinal},
                        new { role = "user", content = commandstr }
                    },
                    temperature = 0.2
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Erro ao acessar a OpenAI API:");
                    Console.WriteLine(responseString);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseString);
                string baseResult = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()
                    .Trim();
                                
                if (baseResult == null || baseResult.Length < 3 || !baseResult.Contains(","))                
                    return resp; // Resposta inválida

                // Pega comando e lingua

                string[] numberPart = baseResult.Split(","); 

                if (numberPart.Length > 0)
                    resp.Result = int.TryParse(numberPart[0].Trim(), out int cmd) ? cmd : 0;

                if (numberPart.Length > 1)
                    resp.Language = numberPart[1].Trim();

                return resp;

            }
            catch (Exception ex)
            {

                Console.WriteLine("Erro ao processar o comando:");
                Console.WriteLine(ex.Message);

                return resp;

            }

        }

        public class FullEvaluation
        {

            public int Result { get; set; } = 0; // 1 a 5

            public string Response { get; set; } = string.Empty;

            public string Language { get; set; } = "";

        }

        public static async Task<FullEvaluation> CheckThread(TwitterAPI.ThreadContext thread)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "Você é um juiz muito duro e rigoroso. Você recebe uma conversa entre duas pessoas em que a primeira pessoa afirma algo e a segunda quer contra-argumentar. Você deve resonder com um número entre 1 e 5 dizendo o quanto a primeira pessoa da conversa está correta. 1 significa que a primeira pessoa tem plena razão e a segunda está errada, até 5 em que a primeira pessoa está totalmente errada e a segunda certa. Inclua após o número sua resposta com tons de ironia e sarcasmo dizendo o resultado da sua avaliação." },
                    new { role = "user", content = thread.GetFullDialog() }
                },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao acessar a OpenAI API:");
                Console.WriteLine(responseString);
                return null;
            }

            using var doc = JsonDocument.Parse(responseString);
            string baseResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();

            FullEvaluation resp = new FullEvaluation();

            string numberPart = baseResult.Substring(0, 1); // Pega apenas o primeiro caractere da resposta

            // Tenta converter a resposta para número
            resp.Result = int.TryParse(numberPart, out int nota) ? nota : -1;
            resp.Response = baseResult.Substring(2).Trim(); // Pega o restante da resposta após o número   

            if (resp.Response.StartsWith("-"))
                resp.Response = resp.Response.Substring(2).Trim();

            return resp;

        }


        public static async Task<string> VariatePhrase(string phrase)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = "Retorne a mesma frase, com o mesmo sentido, na mesma lingua, mas variando um pouco a frase em si, trocando algumas palavras por sinônimos ou invertendo ordem das palavras" },
                    new { role = "user", content = phrase }
                },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Erro ao acessar a OpenAI API:");
                Console.WriteLine(responseString);
                return null;
            }

            using var doc = JsonDocument.Parse(responseString);
            string resp = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();

            if (resp == null || resp == string.Empty || resp.Length < 5)
            {
                return phrase;
            }

            return resp;

        }

    }

}
