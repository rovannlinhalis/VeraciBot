using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tweetinvi;
using System.Security.Cryptography;

namespace VeraciBot
{

    public class TwitterAPI
    {

        public class TwitterUser
        {
            public string Name { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }

        public static async Task<TwitterUser> GetTwitterUserById(string userId)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

            string url = $"https://api.twitter.com/2/users/{userId}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                return new TwitterUser
                {
                    Name = data.GetProperty("name").GetString(),
                    Username = data.GetProperty("username").GetString()
                };
            }

            return null;

        }

        public static async Task<string> GetUsernameById(string id)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

            string url = $"https://api.twitter.com/2/users/{id}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extrai o nome (name) do usuário
            if (root.TryGetProperty("data", out var data))
            {
                return data.GetProperty("username").GetString();
            }

            return null;

        }

        public static async Task<string> GetNameById(string id)
        {

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

            string url = $"https://api.twitter.com/2/users/{id}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extrai o nome (name) do usuário
            if (root.TryGetProperty("data", out var data))
            {
                return data.GetProperty("name").GetString();
            }

            return null;

        }

        public static string RemoveReferences(string text)
        {

            // Regex para encontrar @ seguido de letras, números e underscores
            string standard = @"@\w+";

            // Substitui todas as ocorrências por string vazia
            string result = Regex.Replace(text, standard, "").Trim();

            // Opcional: remover múltiplos espaços que sobraram
            result = Regex.Replace(result, @"\s{2,}", " ");

            return result.Trim();

        }

        /// <summary>
        /// Descreve toda uma thread
        /// </summary>
        public class ThreadContext
        {
            
            public string Id { get; set; } = string.Empty;    // Id do primeiro tweet da thread será usado para identificar a thread

            public string AuthorA { get; set; } = string.Empty;   // Id do autor da thread (primeiro usuário da thread que será o usuário A)
            
            public string AuthorB { get; set; } = string.Empty;   // Id de quem responde a thread (segundo usuário da thread que será o usuário B)
            
            public List<TweetContext> Tweets { get; set; } = new List<TweetContext>(); // Lista de tweets da thread

            /// <summary>
            /// Pega a descrição da thread
            /// </summary>
            /// <returns></returns>
            public string GetFullDialog()
            {

                string description = "";
                foreach (var tweet in Tweets)
                {
                    description += $"{tweet.AuthorUsername}: {tweet.Text}\n";
                }

                return description;
            }

            /// <summary>
            /// Pega inicio da thread para author a
            /// </summary>
            /// <returns></returns>
            public string GetStartA()
            {

                string description = "";
                foreach (var tweet in Tweets)
                {

                    if (tweet.AuthorId == AuthorA && tweet.Text != "")
                    {
                        description = tweet.Text;
                        break;
                    }

                }

                return description;
            }

            /// <summary>
            /// Pega inicio da thread para author b
            /// </summary>
            /// <returns></returns>
            public string GetStartB()
            {

                string description = "";
                foreach (var tweet in Tweets)
                {

                    if (tweet.AuthorId == AuthorB && tweet.Text != "")
                    {
                        description = tweet.Text;
                        break;
                    }

                }

                return description;
            }

        }

        /// <summary>
        /// Pega a thread de um tweet
        /// </summary>
        /// <param name="tweetId"></param>
        /// <param name="authorId"></param>
        /// <returns></returns>
        public static async Task<ThreadContext> GetThreadContext(string tweetId, string authorId)
        {

            TweetContext tw = await GetTweetContext(tweetId);
            if (tw == null)
            {
                Console.WriteLine("Erro ao buscar o tweet.");
                return null;
            }

            if (tw.RepliedToId == "")
            {

                // Se não é resposta a ninguém, então é o primeiro tweet da thread  

                return new ThreadContext
                {
                    Id = tweetId,
                    AuthorA = tw.AuthorId,
                    AuthorB = authorId,
                    Tweets = new List<TweetContext> { tw }
                };
                
            }
            else 
            {

                // Pega a thread que vem antes até aqui

                ThreadContext tc = await GetThreadContext(tw.RepliedToId, authorId);
                if (tc == null)
                {
                    Console.WriteLine("Erro ao buscar a thread.");
                    return null;
                }   

                if (tw.AuthorId == tc.AuthorA || tw.AuthorId == tc.AuthorB)
                {
                    // Se o autor do tweet atual é o mesmo que o autor da thread ou o author da chamada, então adiciona o tweet atual à thread (outros autores são ignorados na thread)
                    tc.Tweets.Add(tw);
                }

                return tc; // Retorna a thread atualizada

            }

        }

        /// <summary>
        /// Descreve um tweet específico
        /// </summary>
        public class TweetContext
        {
            public string Id { get; set; } = string.Empty;  // Id do tweet
            public string AuthorId { get; set; } = string.Empty; //Id do autor do tweet 
            public string AuthorName { get; set; } = string.Empty; // Nome do author do tweet
            public string AuthorUsername { get; set; } = string.Empty; // Nome de usuário do author do tweet (@)
            public string Text { get; set; } = string.Empty; // Texto do tweet
            public string CreatedAt { get; set; } = string.Empty; // Data da criação    
            public string RepliedToId { get; set; } = string.Empty;  // É resposta a tweet
        }

        /// <summary>
        /// Pega infos de um tweet específico
        /// </summary>
        /// <param name="tweetId"></param>
        /// <returns></returns>
        public static async Task<TweetContext> GetTweetContext(string tweetId)
        {

            try
            {

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

                string url = $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=text,author_id,created_at,referenced_tweets&user.fields=username,name";

                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erro ao buscar o tweet original: {response.StatusCode}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement.GetProperty("data");

                string text = RemoveReferences(root.GetProperty("text").GetString());
                string authorId = root.GetProperty("author_id").GetString();
                string createdAt = root.GetProperty("created_at").GetString();
                string authorName = "";
                string authorUsername = "";
                string repliedToId = "";

                if (root.TryGetProperty("includes", out JsonElement includes) &&
                                includes.TryGetProperty("users", out JsonElement users))
                {
                    foreach (var user in users.EnumerateArray())
                    {
                        if (user.GetProperty("id").GetString() == authorId)
                        {
                            authorUsername = user.GetProperty("username").GetString();
                            authorName = user.GetProperty("name").GetString();
                            break;
                        }
                    }
                }

                if (root.TryGetProperty("referenced_tweets", out JsonElement referencedTweets))
                {
                    foreach (var refTweet in referencedTweets.EnumerateArray())
                    {
                        if (refTweet.GetProperty("type").GetString() == "replied_to")
                        {
                            repliedToId = refTweet.GetProperty("id").GetString();
                            break;
                        }
                    }
                }

                // Aqui tenho todas as informações de um tweet

                return new TweetContext
                {
                    Id = tweetId,
                    Text = text,
                    AuthorId = authorId,
                    AuthorName = authorName,
                    AuthorUsername = authorUsername,
                    CreatedAt = createdAt,
                    RepliedToId = repliedToId
                };

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar o tweet: {ex.Message}");
                return null;
            }

        }

        public static async Task<string> GetRepliedTweetText(string tweetId)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

            string url = $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=referenced_tweets";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro ao buscar o tweet: {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement.GetProperty("data");

            if (root.TryGetProperty("referenced_tweets", out JsonElement referencedTweets))
            {
                foreach (var refTweet in referencedTweets.EnumerateArray())
                {
                    if (refTweet.GetProperty("type").GetString() == "replied_to")
                    {
                        string repliedToId = refTweet.GetProperty("id").GetString();
                        return await GetTweetTextById(repliedToId);
                    }
                }
            }

            return null; // Não está respondendo a outro tweet
        }

        public static async Task<string> GetTweetTextById(string tweetId)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppKeys.keys.xBearerToken);

            string url = $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=text";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Erro ao buscar o tweet original: {response.StatusCode}");
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement.GetProperty("data");

            return root.GetProperty("text").GetString();
        }

        public static async Task PostReplyAsync(string message, string replyToTweetId)
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

        public static async Task PostReplyWithImageAsync(string message, string image, string replyToTweetId)
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

        public static async Task<string> UploadImageTweetinviAsync(string image)
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

        public static string GenerateOAuthHeader(string httpMethod, string url, Dictionary<string, string> additionalParams = null, bool isUpload = false)
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

    }

}
