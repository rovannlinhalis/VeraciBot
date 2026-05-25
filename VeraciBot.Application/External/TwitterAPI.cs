using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using Tweetinvi.Parameters.V2;
using VeraciBot.Core.Entities;
using VeraciBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using VeraciBot.Application.Services;

namespace VeraciBot.Application.External
{
    public sealed class TwitterApiException : InvalidOperationException
    {
        public TwitterApiException(
            int statusCode,
            string title,
            string detail,
            string type,
            string accountId,
            string rawContent)
            : base(BuildMessage(statusCode, title, detail, type, accountId, rawContent))
        {
            StatusCode = statusCode;
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            Type = type ?? string.Empty;
            AccountId = accountId ?? string.Empty;
            RawContent = rawContent ?? string.Empty;
        }

        public int StatusCode { get; }
        public string Title { get; }
        public string Detail { get; }
        public string Type { get; }
        public string AccountId { get; }
        public string RawContent { get; }

        public bool IsCreditsDepleted =>
            StatusCode == 402 ||
            string.Equals(Title, "CreditsDepleted", StringComparison.OrdinalIgnoreCase) ||
            Type.Contains("/credits", StringComparison.OrdinalIgnoreCase);

        private static string BuildMessage(
            int statusCode,
            string title,
            string detail,
            string type,
            string accountId,
            string rawContent)
        {
            var isCreditsDepleted =
                statusCode == 402 ||
                string.Equals(title, "CreditsDepleted", StringComparison.OrdinalIgnoreCase) ||
                (type ?? string.Empty).Contains("/credits", StringComparison.OrdinalIgnoreCase);

            if (isCreditsDepleted)
            {
                var accountText = string.IsNullOrWhiteSpace(accountId)
                    ? "A conta/projeto da API do X/Twitter"
                    : $"A conta/projeto da API do X/Twitter {accountId}";

                return $"{accountText} esta sem creditos para esta requisicao. Recarregue creditos, ajuste o plano no portal do X/Twitter ou aguarde a renovacao dos creditos; o worker nao conseguira ler mencoes ate isso ser resolvido.";
            }

            var parts = new[] { title, detail }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var parsedMessage = string.Join(": ", parts);

            if (!string.IsNullOrWhiteSpace(parsedMessage))
                return $"Erro na API do X/Twitter ({statusCode}): {parsedMessage}";

            return $"Erro na API do X/Twitter ({statusCode}): {rawContent}";
        }
    }

    public class TwitterAPI
    {
        private readonly ApplicationSettingsService _applicationSettingsService;
        private readonly TwitterBotAuthenticationService _botAuthenticationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IBlobStorageService _blobStorage;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TwitterAPI> _logger;

        public TwitterAPI(
            ApplicationSettingsService applicationSettingsService,
            TwitterBotAuthenticationService botAuthenticationService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IBlobStorageService blobStorage,
            IWebHostEnvironment environment,
            ILogger<TwitterAPI> logger
        )
        {
            _applicationSettingsService = applicationSettingsService;
            _botAuthenticationService = botAuthenticationService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _blobStorage = blobStorage;
            _environment = environment;
            _logger = logger;
        }

        public class TwitterUser
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
        }

        public sealed class MentionContext
        {
            public string Id { get; set; } = string.Empty;
            public string AuthorId { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public DateTimeOffset CreatedAtUtc { get; set; }
        }

        public sealed class TwitterUsageSummary
        {
            public string ProjectId { get; set; } = string.Empty;
            public long? ProjectCap { get; set; }
            public long? ProjectUsage { get; set; }
            public long? RemainingPosts =>
                ProjectCap.HasValue && ProjectUsage.HasValue
                    ? Math.Max(0, ProjectCap.Value - ProjectUsage.Value)
                    : null;
            public int? CapResetDay { get; set; }
            public DateTimeOffset RetrievedAtUtc { get; set; } = DateTimeOffset.UtcNow;
            public IReadOnlyList<TwitterUsageDay> DailyProjectUsage { get; set; } = [];
        }

        public sealed class TwitterUsageDay
        {
            public DateTimeOffset? Date { get; set; }
            public long Usage { get; set; }
        }

        private sealed class TweetImageContent
        {
            public byte[] Bytes { get; set; } = [];
            public string ContentType { get; set; } = "image/png";
            public string Source { get; set; } = string.Empty;
        }

        public async Task<TwitterUsageSummary> GetUsageSummaryAsync(int days = 30)
        {
            var safeDays = Math.Clamp(days, 1, 90);
            var fields = Uri.EscapeDataString(
                "cap_reset_day,daily_client_app_usage,daily_project_usage,project_cap,project_id,project_usage"
            );
            var url = $"https://api.x.com/2/usage/tweets?days={safeDays}&usage.fields={fields}";

            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            await AddUsageAuthorizationAsync(request);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw CreateTwitterApiException((int)response.StatusCode, content);

            using var doc = JsonDocument.Parse(content);
            return ParseUsageSummary(doc);
        }

        public async Task<IReadOnlyList<MentionContext>> GetMentionsSinceAsync(
            string userId,
            DateTimeOffset startTimeUtc,
            int maxResults = 25,
            string sinceId = ""
        )
        {
            if (string.IsNullOrWhiteSpace(userId))
                return [];

            if (await ShouldUseOAuth2Async())
                return await GetMentionsSinceOAuth2Async(userId, startTimeUtc, maxResults, sinceId);

            var client = await CreateUserTwitterClientAsync();
            var safeMaxResults = Math.Clamp(maxResults, 5, 100);
            var cursorQuery = BuildMentionCursorQuery(startTimeUtc, sinceId);
            var url =
                $"https://api.twitter.com/2/users/{userId}/mentions" +
                $"?tweet.fields=author_id,created_at,text" +
                cursorQuery +
                $"&max_results={safeMaxResults}";

            var result = await client.Execute.RequestAsync(query =>
            {
                query.Url = url;
                query.HttpMethod = Tweetinvi.Models.HttpMethod.GET;
            });

            if (result?.Response is null || !result.Response.IsSuccessStatusCode)
            {
                var statusCode = result?.Response is null ? 0 : (int)result.Response.StatusCode;
                throw CreateTwitterApiException(statusCode, result?.Content ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(result.Content))
                return [];

            var mentions = new List<MentionContext>();
            using var doc = JsonDocument.Parse(result.Content);
            LogMentionResponseMetadata(doc, userId, startTimeUtc, sinceId);
            ThrowIfTwitterErrorsWithoutData(doc);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return mentions;

            foreach (var tweet in data.EnumerateArray())
            {
                if (!tweet.TryGetProperty("id", out var idProp))
                    continue;

                var createdAtRaw = tweet.TryGetProperty("created_at", out var createdAtProp)
                    ? createdAtProp.GetString() ?? string.Empty
                    : string.Empty;

                if (!DateTimeOffset.TryParse(createdAtRaw, out var createdAtUtc))
                    createdAtUtc = DateTimeOffset.UtcNow;

                mentions.Add(new MentionContext
                {
                    Id = idProp.GetString() ?? string.Empty,
                    AuthorId = tweet.TryGetProperty("author_id", out var authorIdProp)
                        ? authorIdProp.GetString() ?? string.Empty
                        : string.Empty,
                    Text = tweet.TryGetProperty("text", out var textProp)
                        ? textProp.GetString() ?? string.Empty
                        : string.Empty,
                    CreatedAtUtc = createdAtUtc
                });
            }

            return mentions;
        }

        public async Task<TwitterUser> GetTwitterUserById(string userId)
        {
            if (await ShouldUseOAuth2Async())
                return await GetTwitterUserByIdOAuth2Async(userId);

            try
            {
                var client = await CreateReadOnlyTwitterClientAsync();
                var response = await client.UsersV2.GetUserByIdAsync(userId);
                var user = response?.User;

                if (user is null)
                {
                    Console.WriteLine("Erro ao buscar usuario por id.");
                    return null;
                }

                return new TwitterUser
                {
                    Id = user.Id ?? string.Empty,
                    Name = user.Name ?? string.Empty,
                    Username = user.Username ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar usuario por id: {ex.Message}");
                return null;
            }

        }


        public async Task<TwitterUser> GetTwitterUserByUserName(string userName)
        {

            if (userName.StartsWith("@"))
            {
                userName = userName.Substring(1);
            }

            if (await ShouldUseOAuth2Async())
                return await GetTwitterUserByUserNameOAuth2Async(userName);

            try
            {
                var client = await CreateReadOnlyTwitterClientAsync();
                var response = await client.UsersV2.GetUserByNameAsync(userName);
                var user = response?.User;

                if (user is null)
                {
                    Console.WriteLine("Erro ao buscar usuario por username.");
                    return null;
                }

                return new TwitterUser
                {
                    Id = user.Id ?? string.Empty,
                    Name = user.Name ?? string.Empty,
                    Username = user.Username ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar usuario por username: {ex.Message}");
                return null;
            }

        }

        public async Task<string> GetUsernameById(string id)
        {
            var user = await GetTwitterUserById(id);
            return user?.Username;

        }

        public async Task<string> GetNameById(string id)
        {
            var user = await GetTwitterUserById(id);
            return user?.Name;

        }

        public static string RemoveReferences(string text)
        {
            return TwitterTextParser.RemoveReferences(text);
        }

        /// <summary>
        /// Finds all user references in the specified text that are prefixed with the '@' symbol.
        /// </summary>
        /// <remarks>User references are identified as sequences that start with '@' followed by one or
        /// more letters, digits, or underscores. The search is case-sensitive and does not validate whether the
        /// referenced users exist.</remarks>
        /// <param name="text">The text to search for user references. May be null or empty.</param>
        /// <returns>An array of strings containing all user references found in the text, each starting with '@'. Returns an
        /// empty array if no user references are found.</returns>
        public static string[] FindUsersReference(string text)
        {
            return TwitterTextParser.FindUsersReference(text);
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
        public async Task<ThreadContext> GetThreadContext(string tweetId, string authorId)
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
        public async Task<TweetContext> GetTweetContext(string tweetId)
        {
            if (await ShouldUseOAuth2Async())
                return await GetTweetContextOAuth2Async(tweetId);

            try
            {
                var client = await CreateReadOnlyTwitterClientAsync();
                var parameters = new GetTweetV2Parameters(tweetId)
                {
                    TweetFields =
                    {
                        TweetResponseFields.Tweet.Text,
                        TweetResponseFields.Tweet.AuthorId,
                        TweetResponseFields.Tweet.CreatedAt,
                        TweetResponseFields.Tweet.ReferencedTweets
                    },
                    Expansions =
                    {
                        TweetResponseFields.Expansions.AuthorId
                    },
                    UserFields =
                    {
                        TweetResponseFields.User.Username,
                        TweetResponseFields.User.Name
                    }
                };

                var response = await client.TweetsV2.GetTweetAsync(parameters);
                var tweet = response?.Tweet;
                if (tweet is null)
                {
                    Console.WriteLine("Erro ao buscar o tweet original.");
                    return null;
                }

                string text = RemoveReferences(tweet.Text ?? string.Empty);
                string authorId = tweet.AuthorId ?? string.Empty;
                string createdAt = tweet.CreatedAt.ToString("O");
                string authorName = "";
                string authorUsername = "";
                string repliedToId = "";

                if (response.Includes?.Users is not null)
                {
                    foreach (var user in response.Includes.Users)
                    {
                        if ((user.Id ?? string.Empty) == authorId)
                        {
                            authorUsername = user.Username ?? string.Empty;
                            authorName = user.Name ?? string.Empty;
                            break;
                        }
                    }
                }

                if (tweet.ReferencedTweets is not null)
                {
                    foreach (var referencedTweet in tweet.ReferencedTweets)
                    {
                        if (string.Equals(referencedTweet.Type, "replied_to", StringComparison.OrdinalIgnoreCase))
                        {
                            repliedToId = referencedTweet.Id ?? string.Empty;
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

        public async Task<string> GetRepliedTweetText(string tweetId)
        {
            var client = await CreateReadOnlyTwitterClientAsync();
            var parameters = new GetTweetV2Parameters(tweetId)
            {
                TweetFields =
                {
                    TweetResponseFields.Tweet.ReferencedTweets
                }
            };

            var response = await client.TweetsV2.GetTweetAsync(parameters);
            var tweet = response?.Tweet;
            if (tweet is null)
            {
                Console.WriteLine("Erro ao buscar o tweet.");
                return null;
            }

            if (tweet.ReferencedTweets is not null)
            {
                foreach (var referencedTweet in tweet.ReferencedTweets)
                {
                    if (string.Equals(referencedTweet.Type, "replied_to", StringComparison.OrdinalIgnoreCase))
                    {
                        string repliedToId = referencedTweet.Id ?? string.Empty;
                        return await GetTweetTextById(repliedToId);
                    }
                }
            }

            return null; // Não está respondendo a outro tweet
        }

        public async Task<string> GetTweetTextById(string tweetId)
        {
            if (await ShouldUseOAuth2Async())
            {
                var oauth2Tweet = await GetTweetContextOAuth2Async(tweetId);
                return oauth2Tweet?.Text ?? string.Empty;
            }

            var client = await CreateReadOnlyTwitterClientAsync();
            var parameters = new GetTweetV2Parameters(tweetId)
            {
                TweetFields =
                {
                    TweetResponseFields.Tweet.Text
                }
            };

            var response = await client.TweetsV2.GetTweetAsync(parameters);
            var tweet = response?.Tweet;
            if (tweet is null)
            {
                Console.WriteLine("Erro ao buscar o tweet original.");
                return null;
            }

            return tweet.Text ?? string.Empty;
        }

        public async Task PostReplyAsync(string message, string replyToTweetId)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException("Mensagem de resposta invalida.");

            if (string.IsNullOrWhiteSpace(replyToTweetId))
                throw new InvalidOperationException("Tweet de resposta invalido.");

            try
            {
                if (await ShouldUseOAuth2Async())
                {
                    await PostReplyOAuth2Async(message, replyToTweetId, mediaId: null);
                    return;
                }

                var client = await CreateUserTwitterClientAsync();

                if (!long.TryParse(replyToTweetId, out var tweetIdNumber))
                {
                    throw new InvalidOperationException("Tweet de resposta invalido.");
                }

                var publishParameters = new PublishTweetParameters(message)
                {
                    InReplyToTweetId = tweetIdNumber,
                    AutoPopulateReplyMetadata = true
                };
                var result = await client.Tweets.PublishTweetAsync(publishParameters);

                if (result is not null)
                {
                    Console.WriteLine("Resposta enviada com sucesso.");
                }
                else
                {
                    throw new InvalidOperationException("Falha ao enviar resposta para o Twitter/X.");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao publicar resposta no Twitter/X para {TweetId}.", replyToTweetId);
                throw;
            }

        }

        public async Task PostReplyWithImageAsync(string message, string image, string replyToTweetId)
        {

            try
            {
                var imageContent = await ReadTweetImageContentAsync(image);
                if (imageContent is null)
                {
                    _logger.LogWarning("Imagem {Image} nao encontrada. Publicando resposta sem imagem.", image);
                    await PostReplyAsync(message, replyToTweetId);
                    return;
                }

                if (await ShouldUseOAuth2Async())
                {
                    var mediaId = await UploadTweetImageOAuth2Async(imageContent);
                    await PostReplyOAuth2Async(message, replyToTweetId, mediaId);
                    return;
                }

                var client = await CreateUserTwitterClientAsync();

                if (!long.TryParse(replyToTweetId, out var tweetIdNumber))
                {
                    throw new InvalidOperationException("Tweet de resposta invalido.");
                }

                var media = await client.Upload.UploadTweetImageAsync(imageContent.Bytes);

                var publishParameters = new PublishTweetParameters(message)
                {
                    InReplyToTweetId = tweetIdNumber,
                    AutoPopulateReplyMetadata = true,
                    Medias = new List<IMedia> { media }
                };
                var result = await client.Tweets.PublishTweetAsync(publishParameters);

                if (result is not null)
                {
                    Console.WriteLine("Resposta enviada com imagem.");
                }
                else
                {
                    throw new InvalidOperationException("Falha ao enviar resposta com imagem para o Twitter/X.");
                }


            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao publicar resposta com imagem {Image}. Tentando publicar sem imagem.", image);
                await PostReplyAsync(message, replyToTweetId);

            }

        }

        private async Task<bool> ShouldUseOAuth2Async()
        {
            if (await _botAuthenticationService.IsOAuth2ModeAsync())
                return true;

            var accessToken = await _applicationSettingsService.GetValueAsync(ApplicationParameter.TWITTER_ACCESS_TOKEN);
            var accessSecret = await _applicationSettingsService.GetValueAsync(ApplicationParameter.TWITTER_ACCESS_SECRET);
            var refreshToken = await _applicationSettingsService.GetValueAsync(ApplicationParameter.TWITTER_REFRESH_TOKEN);

            return !string.IsNullOrWhiteSpace(refreshToken)
                || (!string.IsNullOrWhiteSpace(accessToken) && string.IsNullOrWhiteSpace(accessSecret));
        }

        private async Task<IReadOnlyList<MentionContext>> GetMentionsSinceOAuth2Async(
            string userId,
            DateTimeOffset startTimeUtc,
            int maxResults,
            string sinceId)
        {
            var safeMaxResults = Math.Clamp(maxResults, 5, 100);
            var cursorQuery = BuildMentionCursorQuery(startTimeUtc, sinceId);
            var url =
                $"https://api.x.com/2/users/{userId}/mentions" +
                $"?tweet.fields=author_id,created_at,text" +
                cursorQuery +
                $"&max_results={safeMaxResults}";

            using var doc = await GetOAuth2JsonAsync(url);
            if (doc is null)
                return [];

            var mentions = new List<MentionContext>();
            LogMentionResponseMetadata(doc, userId, startTimeUtc, sinceId);
            ThrowIfTwitterErrorsWithoutData(doc);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return mentions;

            foreach (var tweet in data.EnumerateArray())
            {
                if (!tweet.TryGetProperty("id", out var idProp))
                    continue;

                var createdAtRaw = tweet.TryGetProperty("created_at", out var createdAtProp)
                    ? createdAtProp.GetString() ?? string.Empty
                    : string.Empty;

                if (!DateTimeOffset.TryParse(createdAtRaw, out var createdAtUtc))
                    createdAtUtc = DateTimeOffset.UtcNow;

                mentions.Add(new MentionContext
                {
                    Id = idProp.GetString() ?? string.Empty,
                    AuthorId = tweet.TryGetProperty("author_id", out var authorIdProp)
                        ? authorIdProp.GetString() ?? string.Empty
                        : string.Empty,
                    Text = tweet.TryGetProperty("text", out var textProp)
                        ? textProp.GetString() ?? string.Empty
                        : string.Empty,
                    CreatedAtUtc = createdAtUtc
                });
            }

            return mentions;
        }

        private static string BuildMentionCursorQuery(DateTimeOffset startTimeUtc, string sinceId)
        {
            if (!string.IsNullOrWhiteSpace(sinceId))
                return $"&since_id={Uri.EscapeDataString(sinceId.Trim())}";

            var encodedStartTime = Uri.EscapeDataString(startTimeUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return $"&start_time={encodedStartTime}";
        }

        private void LogMentionResponseMetadata(
            JsonDocument doc,
            string userId,
            DateTimeOffset startTimeUtc,
            string sinceId)
        {
            if (doc is null || !doc.RootElement.TryGetProperty("meta", out var meta))
                return;

            var resultCount = meta.TryGetProperty("result_count", out var resultCountProp)
                ? resultCountProp.GetInt32()
                : 0;
            var newestId = meta.TryGetProperty("newest_id", out var newestIdProp)
                ? newestIdProp.GetString() ?? string.Empty
                : string.Empty;
            var oldestId = meta.TryGetProperty("oldest_id", out var oldestIdProp)
                ? oldestIdProp.GetString() ?? string.Empty
                : string.Empty;

            _logger.LogInformation(
                "Consulta de mencoes retornou {ResultCount} itens para user {UserId}. SinceId={SinceId}; StartTimeUtc={StartTimeUtc}; NewestId={NewestId}; OldestId={OldestId}.",
                resultCount,
                userId,
                string.IsNullOrWhiteSpace(sinceId) ? "-" : sinceId,
                startTimeUtc.UtcDateTime.ToString("O"),
                string.IsNullOrWhiteSpace(newestId) ? "-" : newestId,
                string.IsNullOrWhiteSpace(oldestId) ? "-" : oldestId
            );
        }

        private static void ThrowIfTwitterErrorsWithoutData(JsonDocument doc)
        {
            if (doc is null || doc.RootElement.TryGetProperty("data", out _))
                return;

            if (!doc.RootElement.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
                return;

            var messages = errors
                .EnumerateArray()
                .Select(error =>
                {
                    var title = error.TryGetProperty("title", out var titleProp)
                        ? titleProp.GetString()
                        : null;
                    var detail = error.TryGetProperty("detail", out var detailProp)
                        ? detailProp.GetString()
                        : null;
                    return string.Join(": ", new[] { title, detail }.Where(x => !string.IsNullOrWhiteSpace(x)));
                })
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var message = string.Join("; ", messages);
            if (!string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException($"O X/Twitter retornou erro ao buscar mencoes: {message}");
        }

        private async Task<TwitterUser> GetTwitterUserByIdOAuth2Async(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            using var doc = await GetOAuth2JsonAsync($"https://api.x.com/2/users/{userId}?user.fields=username,name");
            return ParseOAuth2User(doc);
        }

        private async Task<TwitterUser> GetTwitterUserByUserNameOAuth2Async(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            using var doc = await GetOAuth2JsonAsync($"https://api.x.com/2/users/by/username/{Uri.EscapeDataString(userName)}?user.fields=username,name");
            return ParseOAuth2User(doc);
        }

        private async Task<TweetContext> GetTweetContextOAuth2Async(string tweetId)
        {
            if (string.IsNullOrWhiteSpace(tweetId))
                return null;

            var url =
                $"https://api.x.com/2/tweets/{tweetId}" +
                "?tweet.fields=text,author_id,created_at,referenced_tweets" +
                "&expansions=author_id" +
                "&user.fields=username,name";

            using var doc = await GetOAuth2JsonAsync(url);
            if (doc is null || !doc.RootElement.TryGetProperty("data", out var tweet))
                return null;

            var authorId = tweet.TryGetProperty("author_id", out var authorIdProp)
                ? authorIdProp.GetString() ?? string.Empty
                : string.Empty;
            var text = tweet.TryGetProperty("text", out var textProp)
                ? RemoveReferences(textProp.GetString() ?? string.Empty)
                : string.Empty;
            var createdAt = tweet.TryGetProperty("created_at", out var createdAtProp)
                ? createdAtProp.GetString() ?? string.Empty
                : string.Empty;
            var repliedToId = string.Empty;
            var authorName = string.Empty;
            var authorUsername = string.Empty;

            if (tweet.TryGetProperty("referenced_tweets", out var references) && references.ValueKind == JsonValueKind.Array)
            {
                foreach (var reference in references.EnumerateArray())
                {
                    var type = reference.TryGetProperty("type", out var typeProp)
                        ? typeProp.GetString() ?? string.Empty
                        : string.Empty;
                    if (string.Equals(type, "replied_to", StringComparison.OrdinalIgnoreCase))
                    {
                        repliedToId = reference.TryGetProperty("id", out var idProp)
                            ? idProp.GetString() ?? string.Empty
                            : string.Empty;
                        break;
                    }
                }
            }

            if (doc.RootElement.TryGetProperty("includes", out var includes)
                && includes.TryGetProperty("users", out var users)
                && users.ValueKind == JsonValueKind.Array)
            {
                foreach (var user in users.EnumerateArray())
                {
                    var id = user.TryGetProperty("id", out var idProp)
                        ? idProp.GetString() ?? string.Empty
                        : string.Empty;
                    if (id != authorId)
                        continue;

                    authorUsername = user.TryGetProperty("username", out var usernameProp)
                        ? usernameProp.GetString() ?? string.Empty
                        : string.Empty;
                    authorName = user.TryGetProperty("name", out var nameProp)
                        ? nameProp.GetString() ?? string.Empty
                        : string.Empty;
                    break;
                }
            }

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

        private async Task PostReplyOAuth2Async(string message, string replyToTweetId, string mediaId)
        {
            if (string.IsNullOrWhiteSpace(replyToTweetId))
                throw new InvalidOperationException("Tweet de resposta invalido.");

            var payload = new Dictionary<string, object>
            {
                ["text"] = message,
                ["reply"] = new Dictionary<string, object>
                {
                    ["in_reply_to_tweet_id"] = replyToTweetId
                }
            };

            if (!string.IsNullOrWhiteSpace(mediaId))
            {
                payload["media"] = new Dictionary<string, object>
                {
                    ["media_ids"] = new[] { mediaId }
                };
            }

            using var doc = await SendOAuth2JsonAsync(System.Net.Http.HttpMethod.Post, "https://api.x.com/2/tweets", payload);
            if (doc is not null)
                Console.WriteLine("Resposta enviada com sucesso.");
        }

        private async Task<string> UploadTweetImageOAuth2Async(TweetImageContent image)
        {
            if (image is null || image.Bytes.Length == 0)
                throw new InvalidOperationException("Arquivo de imagem nao encontrado.");

            var payload = new Dictionary<string, object>
            {
                ["media"] = Convert.ToBase64String(image.Bytes),
                ["media_category"] = "tweet_image",
                ["media_type"] = image.ContentType
            };

            using var doc = await SendOAuth2JsonAsync(System.Net.Http.HttpMethod.Post, "https://api.x.com/2/media/upload", payload);
            if (doc is null || !doc.RootElement.TryGetProperty("data", out var data))
                throw new InvalidOperationException("O X/Twitter nao retornou o media id.");

            if (data.TryGetProperty("id", out var idProp) && !string.IsNullOrWhiteSpace(idProp.GetString()))
                return idProp.GetString();

            if (data.TryGetProperty("media_key", out var mediaKeyProp) && !string.IsNullOrWhiteSpace(mediaKeyProp.GetString()))
                return mediaKeyProp.GetString();

            throw new InvalidOperationException("O X/Twitter nao retornou o media id.");
        }

        private async Task<TweetImageContent> ReadTweetImageContentAsync(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
                return null;

            var localPath = TryResolveLocalFilePath(image);
            if (!string.IsNullOrWhiteSpace(localPath))
                return await ReadLocalTweetImageContentAsync(localPath, image);

            var blobContent = await TryReadBlobTweetImageContentAsync(image);
            if (blobContent is not null)
                return blobContent;

            localPath = TryResolveWebRootFilePath(image);
            if (!string.IsNullOrWhiteSpace(localPath))
                return await ReadLocalTweetImageContentAsync(localPath, image);

            if (Uri.TryCreate(image, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return await DownloadTweetImageContentAsync(uri);
            }

            return null;
        }

        private async Task<TweetImageContent> ReadLocalTweetImageContentAsync(string path, string source)
        {
            return new TweetImageContent
            {
                Bytes = await File.ReadAllBytesAsync(path),
                ContentType = GetMediaContentType(path),
                Source = source
            };
        }

        private async Task<TweetImageContent> TryReadBlobTweetImageContentAsync(string image)
        {
            try
            {
                if (!await _blobStorage.FileExists(image))
                    return null;

                await using var stream = await _blobStorage.OpenFileMemory(image);
                return new TweetImageContent
                {
                    Bytes = stream.ToArray(),
                    ContentType = GetMediaContentType(image),
                    Source = image
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Imagem {Image} nao foi resolvida pelo blob storage.", image);
                return null;
            }
        }

        private async Task<TweetImageContent> DownloadTweetImageContentAsync(Uri uri)
        {
            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, uri);
            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? GetMediaContentType(uri.AbsolutePath);
            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            return new TweetImageContent
            {
                Bytes = await response.Content.ReadAsByteArrayAsync(),
                ContentType = contentType,
                Source = uri.ToString()
            };
        }

        private static string TryResolveLocalFilePath(string image)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(image))
                    return null;

                if (Path.IsPathRooted(image))
                {
                    var path = Path.GetFullPath(image);
                    return File.Exists(path) ? path : null;
                }

                var relativePath = Path.GetFullPath(image);
                return File.Exists(relativePath) ? relativePath : null;
            }
            catch
            {
                return null;
            }
        }

        private string TryResolveWebRootFilePath(string image)
        {
            try
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var relativePath = NormalizeWebRootRelativePath(image);
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var rootPath = Path.GetFullPath(webRootPath);
                var rootPathWithSeparator = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var path = Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));

                if (!path.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
                    return null;

                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeWebRootRelativePath(string image)
        {
            if (string.IsNullOrWhiteSpace(image))
                return string.Empty;

            var normalized = image.Trim().Replace('\\', '/');
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                normalized = uri.AbsolutePath;

            normalized = normalized.TrimStart('/');

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0 || segments.Any(x => x == "." || x == ".."))
                return string.Empty;

            return string.Join('/', segments);
        }

        private static TwitterUsageSummary ParseUsageSummary(JsonDocument doc)
        {
            if (doc is null || !doc.RootElement.TryGetProperty("data", out var data))
                return new TwitterUsageSummary();

            var dailyProjectUsage = ReadDailyProjectUsage(data);
            var projectUsage = ReadOptionalLong(data, "project_usage");
            var capResetDayValue = ReadOptionalLong(data, "cap_reset_day");

            if (!projectUsage.HasValue && dailyProjectUsage.Count > 0)
                projectUsage = dailyProjectUsage.Sum(x => x.Usage);

            return new TwitterUsageSummary
            {
                ProjectId = ReadJsonProperty(data, "project_id"),
                ProjectCap = ReadOptionalLong(data, "project_cap"),
                ProjectUsage = projectUsage,
                CapResetDay = capResetDayValue.HasValue ? (int)capResetDayValue.Value : null,
                RetrievedAtUtc = DateTimeOffset.UtcNow,
                DailyProjectUsage = dailyProjectUsage
            };
        }

        private static IReadOnlyList<TwitterUsageDay> ReadDailyProjectUsage(JsonElement data)
        {
            var days = new List<TwitterUsageDay>();

            if (!data.TryGetProperty("daily_project_usage", out var dailyProjectUsage))
                return days;

            if (dailyProjectUsage.ValueKind == JsonValueKind.Object &&
                dailyProjectUsage.TryGetProperty("usage", out var objectUsage))
            {
                AppendUsageEntries(objectUsage, days);
            }
            else if (dailyProjectUsage.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in dailyProjectUsage.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                        continue;

                    var date = ReadOptionalDate(entry, "date");

                    if (entry.TryGetProperty("usage", out var entryUsage) &&
                        entryUsage.ValueKind == JsonValueKind.Array)
                    {
                        var total = entryUsage
                            .EnumerateArray()
                            .Select(x => ReadOptionalLong(x, "tweets_consumed") ?? ReadOptionalLong(x, "usage") ?? 0)
                            .Sum();

                        days.Add(new TwitterUsageDay { Date = date, Usage = total });
                    }
                    else if (entry.TryGetProperty("usage", out entryUsage))
                    {
                        days.Add(new TwitterUsageDay { Date = date, Usage = ReadJsonLong(entryUsage) ?? 0 });
                    }
                }
            }

            return days
                .OrderBy(x => x.Date ?? DateTimeOffset.MinValue)
                .ToList();
        }

        private static void AppendUsageEntries(JsonElement usage, List<TwitterUsageDay> days)
        {
            if (usage.ValueKind != JsonValueKind.Array)
                return;

            foreach (var entry in usage.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    continue;

                days.Add(new TwitterUsageDay
                {
                    Date = ReadOptionalDate(entry, "date"),
                    Usage = ReadOptionalLong(entry, "usage") ?? ReadOptionalLong(entry, "tweets_consumed") ?? 0
                });
            }
        }

        private static DateTimeOffset? ReadOptionalDate(JsonElement element, string propertyName)
        {
            var raw = ReadJsonProperty(element, propertyName);
            return DateTimeOffset.TryParse(raw, out var value) ? value : null;
        }

        private static long? ReadOptionalLong(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return null;

            return ReadJsonLong(property);
        }

        private static long? ReadJsonLong(JsonElement property)
        {
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
                return number;

            if (property.ValueKind == JsonValueKind.String &&
                long.TryParse(property.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private async Task<JsonDocument> GetOAuth2JsonAsync(string url)
        {
            using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
            await AddOAuth2AuthorizationAsync(request);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw CreateTwitterApiException((int)response.StatusCode, content);
            }

            return JsonDocument.Parse(content);
        }

        private async Task<JsonDocument> SendOAuth2JsonAsync(System.Net.Http.HttpMethod method, string url, object payload)
        {
            using var request = new HttpRequestMessage(method, url);
            await AddOAuth2AuthorizationAsync(request);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw CreateTwitterApiException((int)response.StatusCode, content);

            return string.IsNullOrWhiteSpace(content) ? null : JsonDocument.Parse(content);
        }

        private static TwitterApiException CreateTwitterApiException(int statusCode, string content)
        {
            var title = string.Empty;
            var detail = string.Empty;
            var type = string.Empty;
            var accountId = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    title = ReadJsonProperty(root, "title");
                    detail = ReadJsonProperty(root, "detail");
                    type = ReadJsonProperty(root, "type");
                    accountId = ReadJsonProperty(root, "account_id");

                    if (root.TryGetProperty("errors", out var errors) &&
                        errors.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var error in errors.EnumerateArray())
                        {
                            if (string.IsNullOrWhiteSpace(title))
                                title = ReadJsonProperty(error, "title");

                            if (string.IsNullOrWhiteSpace(detail))
                                detail = ReadJsonProperty(error, "detail");

                            if (string.IsNullOrWhiteSpace(type))
                                type = ReadJsonProperty(error, "type");

                            break;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Mantem o conteudo bruto na mensagem.
            }

            return new TwitterApiException(statusCode, title, detail, type, accountId, content);
        }

        private static string ReadJsonProperty(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
                return string.Empty;

            return property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : property.GetRawText().Trim('"');
        }

        private async Task AddOAuth2AuthorizationAsync(HttpRequestMessage request)
        {
            var accessToken = await _botAuthenticationService.GetOAuth2AccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        private async Task AddUsageAuthorizationAsync(HttpRequestMessage request)
        {
            var bearerToken = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_BEARER_TOKEN,
                "TwitterApi:BearerToken");

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                return;
            }

            await AddOAuth2AuthorizationAsync(request);
        }

        private static TwitterUser ParseOAuth2User(JsonDocument doc)
        {
            if (doc is null || !doc.RootElement.TryGetProperty("data", out var user))
                return null;

            return new TwitterUser
            {
                Id = user.TryGetProperty("id", out var idProp)
                    ? idProp.GetString() ?? string.Empty
                    : string.Empty,
                Name = user.TryGetProperty("name", out var nameProp)
                    ? nameProp.GetString() ?? string.Empty
                    : string.Empty,
                Username = user.TryGetProperty("username", out var usernameProp)
                    ? usernameProp.GetString() ?? string.Empty
                    : string.Empty
            };
        }

        private static string GetMediaContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/png"
            };
        }

        private async Task<TwitterClient> CreateReadOnlyTwitterClientAsync()
        {
            var apiKey = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_CLIENT_ID, "TwitterApi:ApiKey");
            var apiSecret = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_CLIENT_SECRET, "TwitterApi:ApiSecret");

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException(
                    "Configure TWITTER_API_KEY e TWITTER_API_SECRET em ApplicationSettings."
                );
            }

            return new TwitterClient(apiKey, apiSecret);
        }

        private async Task<TwitterClient> CreateUserTwitterClientAsync()
        {
            var apiKey = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_CLIENT_ID, "TwitterApi:ApiKey");
            var apiSecret = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_CLIENT_SECRET, "TwitterApi:ApiSecret");
            var accessToken = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_ACCESS_TOKEN, "TwitterApi:AccessToken");
            var accessSecret = await GetSettingOrConfigAsync(ApplicationParameter.TWITTER_ACCESS_SECRET, "TwitterApi:AccessSecret");

            if (string.IsNullOrWhiteSpace(apiKey)
                || string.IsNullOrWhiteSpace(apiSecret)
                || string.IsNullOrWhiteSpace(accessToken)
                || string.IsNullOrWhiteSpace(accessSecret))
            {
                throw new InvalidOperationException(
                    "Configure TWITTER_API_KEY, TWITTER_API_SECRET, TWITTER_ACCESS_TOKEN e TWITTER_ACCESS_SECRET em ApplicationSettings."
                );
            }

            return new TwitterClient(apiKey, apiSecret, accessToken, accessSecret);
        }

        private async Task<string> GetSettingOrConfigAsync(ApplicationParameter parameter, string appSettingsKey)
        {
            var fromSettings = await _applicationSettingsService.GetValueAsync(parameter);
            if (!string.IsNullOrWhiteSpace(fromSettings))
                return fromSettings;

            return _configuration[appSettingsKey] ?? string.Empty;
        }

    }

}
