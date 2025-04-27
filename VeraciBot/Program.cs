using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net.Http.Headers;
using Tweetinvi.Core.Models;
using System.Linq.Expressions;
using System.Text;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using Tweetinvi.Parameters;
using Tweetinvi;
using System.Text.Json;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.Extensions.DependencyInjection;


namespace VeraciBot
{

    class Program
    {

        private const string IMAGEM_CAMINHO = "img/resposta.jpg";
        private const string MENSAGEM_RESPOSTA = "Teste de resposta";

        static async Task Main(string[] args)
        {

            Console.WriteLine("Starting VERACIBOT"); 

            Console.WriteLine("Connecting VERACIBOT database");

            var services = new ServiceCollection();

            services.AddDbContext<VeraciDb>(options =>
                options.UseSqlServer(AppKeys.keys.dbConnection));

            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<VeraciDb>();

            Console.WriteLine("Starting VERACIBOT bot");

            string startTime = DateTime.UtcNow.AddHours(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            Console.WriteLine("Checking mentions to @veracibot since " + startTime);

            while (true)
            {

                try
                {

                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

                    string mentionsUrl = $"https://api.twitter.com/2/users/{AppKeys.keys.xUserId}/mentions?tweet.fields=author_id,created_at&start_time={startTime}";

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

                        foreach (var tweet in tweets)
                        {
                            string tweetId = tweet["id"].ToString();
                            Console.WriteLine($"Responding tweet {tweetId}...");

                            await PostReplyWithImageAsync(MENSAGEM_RESPOSTA, IMAGEM_CAMINHO, tweetId);
                        }

                    }

                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.ToString());

                }

                Thread.Sleep(600000);

            }

        }

        static async Task PostReplyAsync(string message, string replyToTweetId)
        {

            try
            {

                var client = new HttpClient();

                string url = "https://api.twitter.com/2/tweets";
                string nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
                string timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

                // Parâmetros do corpo (JSON)
                string bodyJson = "{\"text\": \"" + message + "\",\"reply\": {\"in_reply_to_tweet_id\": \"" + replyToTweetId + "\"}}";
                var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

                string authHeader = GenerateOAuthHeader("POST", url);

                var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authHeader.Substring(6)); // Remove "OAuth " duplicado
                request.Content = new ByteArrayContent(bodyBytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    Console.WriteLine("Resposta enviada com sucesso.");
                else
                    Console.WriteLine($"Falha ao enviar resposta: {response.StatusCode} - {result}");

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());

            }

        }

        static async Task PostReplyWithImageAsync(string message, string image, string replyToTweetId)
        {

            try
            {

                string mediaId = await UploadImageTweetinviAsync(image);
                if (mediaId == null)
                {
                    Console.WriteLine("Falha ao enviar imagem.");
                    return;
                }

                // --- Enviar Tweet com Imagem (v2) ---
                var client = new HttpClient();

                string url = "https://api.twitter.com/2/tweets";

                var body = new
                {
                    text = message,
                    reply = new { in_reply_to_tweet_id = replyToTweetId },
                    media = new { media_ids = new[] { mediaId } }
                };

                string bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(body);
                var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

                var authHeader = GenerateOAuthHeader("POST", url);

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authHeader.Substring(6));
                request.Content = new ByteArrayContent(bodyBytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                string result = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    Console.WriteLine("Resposta enviada com imagem.");
                else
                    Console.WriteLine($"Erro ao enviar resposta: {response.StatusCode}\n{result}");


            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());

            }

        }

        static async Task<string> UploadImageTweetinviAsync(string image)
        {

            var client = new TwitterClient(AppKeys.keys.xApiKey, AppKeys.keys.xApiSecret, AppKeys.keys.xAccessToken, AppKeys.keys.xAccessSecret);

            try
            {
                // Upload da imagem
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(image);
                var uploadedImage = await client.Upload.UploadBinaryAsync(imageBytes);

                if (uploadedImage == null)
                {
                    Console.WriteLine("Erro ao fazer upload da imagem.");
                    return null;
                }

                return uploadedImage.UploadedMediaInfo.MediaIdStr;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }

            return null;

        }

        static string GenerateOAuthHeader(string httpMethod, string url, Dictionary<string, string> additionalParams = null, bool isUpload = false)
        {

            string nonce = Convert.ToBase64String(new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            string timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();

            // Parâmetros OAuth
            var oauthParams = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", AppKeys.keys.xApiKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", timestamp },
                { "oauth_token", AppKeys.keys.xAccessToken },
                { "oauth_version", "1.0" }
            };

            // NÃO incluir media_data aqui!
            var signatureParams = new SortedDictionary<string, string>(oauthParams);

            if (additionalParams != null && !isUpload)
            {
                foreach (var pair in additionalParams)
                    signatureParams[pair.Key] = pair.Value;
            }

            string baseStringParams = string.Join("&", signatureParams
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            string signatureBase = $"{httpMethod.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(baseStringParams)}";
            string signingKey = $"{Uri.EscapeDataString(AppKeys.keys.xApiSecret)}&{Uri.EscapeDataString(AppKeys.keys.xAccessSecret)}";

            using var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
            string signature = Convert.ToBase64String(hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));

            oauthParams["oauth_signature"] = signature;

            string header = "OAuth " + string.Join(", ", oauthParams
                .Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\""));

            return header;

        }

        public static async Task<int> AvaliarVeracidadeAsync(string afirmacao)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.openAIKey);

            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                new { role = "system", content = "Você é um verificador de fatos. Para cada afirmação recebida, responda apenas com um número de 1 a 5 representando o grau de veracidade:\n1 - completamente falso/impossível\n2 - provavelmente falso\n3 - incerto/sem dados suficientes\n4 - provavelmente verdadeiro\n5 - verdade factual." },
                new { role = "user", content = afirmacao }
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
                return -1;
            }

            using var doc = JsonDocument.Parse(responseString);
            string resposta = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                .Trim();

            // Tenta converter a resposta para número
            return int.TryParse(resposta, out int nota) ? nota : -1;

        }

    }

}