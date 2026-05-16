using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using VeraciBot.App.Data;
using VeraciBot.App.Entities;

namespace VeraciBot.App.Services
{
    public class TwitterBotAuthenticationService
    {
        private const string OAuth2Mode = "OAuth2";
        private const string AuthorizeUrl = "https://x.com/i/oauth2/authorize";
        private const string TokenUrl = "https://api.x.com/2/oauth2/token";
        private const string MeUrl = "https://api.x.com/2/users/me?user.fields=username,name";

        private readonly ApplicationDbContext _dbContext;
        private readonly ApplicationSettingsService _settings;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public TwitterBotAuthenticationService(
            ApplicationDbContext dbContext,
            ApplicationSettingsService settings,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _settings = settings;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TwitterBotAuthenticationStatus> GetStatusAsync()
        {
            var clientId = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_CLIENT_ID,
                "Authentication:Twitter:ClientId");
            var clientSecret = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_CLIENT_SECRET,
                "Authentication:Twitter:ClientSecret");
            var accessToken = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_ACCESS_TOKEN);
            var refreshToken = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_REFRESH_TOKEN);
            var userId = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_USER_ID);
            var username = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_BOT_USERNAME);
            var name = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_BOT_NAME);
            var authorizedAtRaw = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_BOT_AUTHORIZED_AT_UTC);
            var botAuthorizedRaw = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_BOT_AUTHORIZED);
            var oauthMode = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_OAUTH_MODE);
            var expiresAtRaw = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC);

            DateTimeOffset? authorizedAtUtc = null;
            if (DateTimeOffset.TryParse(authorizedAtRaw, out var parsedAuthorizedAt))
                authorizedAtUtc = parsedAuthorizedAt.ToUniversalTime();

            DateTimeOffset? expiresAtUtc = null;
            if (DateTimeOffset.TryParse(expiresAtRaw, out var parsedExpiresAt))
                expiresAtUtc = parsedExpiresAt.ToUniversalTime();

            return new TwitterBotAuthenticationStatus
            {
                HasApplicationCredentials = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret),
                IsAuthenticated = string.Equals(oauthMode, OAuth2Mode, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(accessToken)
                    && !string.IsNullOrWhiteSpace(userId),
                IsInternallyAuthorized = ParseYesNo(botAuthorizedRaw),
                UserId = userId?.Trim() ?? string.Empty,
                Username = username?.Trim() ?? string.Empty,
                Name = name?.Trim() ?? string.Empty,
                AuthorizedAtUtc = authorizedAtUtc,
                AccessTokenExpiresAtUtc = expiresAtUtc,
                HasRefreshToken = !string.IsNullOrWhiteSpace(refreshToken)
            };
        }

        public async Task<string> BeginAuthorizationAsync(string callbackUrl)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new InvalidOperationException("Callback OAuth invalido.");

            var (clientId, _) = await GetApplicationCredentialsAsync();
            var state = CreateBase64UrlSecret(32);
            var codeVerifier = CreateBase64UrlSecret(64);
            var codeChallenge = CreateCodeChallenge(codeVerifier);
            var scopes = await GetOAuth2ScopesAsync();

            await UpsertSettingAsync(ApplicationParameter.TWITTER_OAUTH2_STATE, state, EFieldType.System);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_OAUTH2_CODE_VERIFIER, codeVerifier, EFieldType.System);
            await _dbContext.SaveChangesAsync();

            return QueryHelpers.AddQueryString(AuthorizeUrl, new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = callbackUrl,
                ["scope"] = scopes,
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256"
            });
        }

        public async Task<TwitterBotAuthenticationResult> CompleteAuthorizationAsync(
            string code,
            string state,
            string callbackUrl,
            string authorizedById)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                throw new InvalidOperationException("Retorno OAuth 2.0 incompleto.");

            var expectedState = await GetStoredSettingValueAsync(ApplicationParameter.TWITTER_OAUTH2_STATE);
            var codeVerifier = await GetStoredSettingValueAsync(ApplicationParameter.TWITTER_OAUTH2_CODE_VERIFIER);

            if (string.IsNullOrWhiteSpace(expectedState) || string.IsNullOrWhiteSpace(codeVerifier))
                throw new InvalidOperationException("Solicitacao OAuth 2.0 nao encontrada. Inicie a autenticacao novamente.");

            if (!string.Equals(state.Trim(), expectedState.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("State OAuth 2.0 invalido. Inicie a autenticacao novamente.");

            var tokenResponse = await RequestOAuth2TokenAsync(new Dictionary<string, string>
            {
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = callbackUrl,
                ["code_verifier"] = codeVerifier
            });

            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                throw new InvalidOperationException("O X/Twitter nao retornou o access token OAuth 2.0 da conta bot.");

            var botUser = await GetAuthenticatedUserAsync(tokenResponse.AccessToken);
            var authorizedAtUtc = DateTimeOffset.UtcNow;
            var expiresAtUtc = CalculateExpiresAtUtc(tokenResponse.ExpiresIn);

            await PersistOAuth2TokenAsync(tokenResponse, expiresAtUtc);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_USER_ID, botUser.Id, EFieldType.SmallText);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_BOT_USERNAME, botUser.Username, EFieldType.Computed);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_BOT_NAME, botUser.Name, EFieldType.Computed);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_BOT_AUTHORIZED_AT_UTC, authorizedAtUtc.ToString("O"), EFieldType.Computed);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_BOT_AUTHORIZED, "1", EFieldType.Computed);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_BOT_AUTHORIZED_BY_ID, authorizedById, EFieldType.System);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_OAUTH2_STATE, string.Empty, EFieldType.System);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_OAUTH2_CODE_VERIFIER, string.Empty, EFieldType.System);
            await _dbContext.SaveChangesAsync();

            return new TwitterBotAuthenticationResult
            {
                UserId = botUser.Id,
                Username = botUser.Username,
                Name = botUser.Name,
                AuthorizedAtUtc = authorizedAtUtc
            };
        }

        public async Task<bool> IsOAuth2ModeAsync()
        {
            var mode = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_OAUTH_MODE);
            return string.Equals(mode, OAuth2Mode, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GetOAuth2AccessTokenAsync()
        {
            var accessToken = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_ACCESS_TOKEN);
            var refreshToken = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_REFRESH_TOKEN);
            var expiresAtRaw = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC);

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("A conta bot ainda nao possui access token OAuth 2.0.");

            if (!DateTimeOffset.TryParse(expiresAtRaw, out var expiresAtUtc)
                || expiresAtUtc.ToUniversalTime() > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return accessToken;
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new InvalidOperationException("O access token OAuth 2.0 expirou e nao ha refresh token. Reautentique a conta bot.");

            var tokenResponse = await RequestOAuth2TokenAsync(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            });

            if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                throw new InvalidOperationException("O X/Twitter nao retornou um novo access token OAuth 2.0.");

            await PersistOAuth2TokenAsync(tokenResponse, CalculateExpiresAtUtc(tokenResponse.ExpiresIn));
            await _dbContext.SaveChangesAsync();

            return tokenResponse.AccessToken;
        }

        private async Task<OAuth2UserData> GetAuthenticatedUserAsync(string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, MeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Nao foi possivel identificar a conta autenticada no X/Twitter: {content}");

            var me = JsonSerializer.Deserialize<OAuth2MeResponse>(content);
            if (me?.Data is null || string.IsNullOrWhiteSpace(me.Data.Id))
                throw new InvalidOperationException("O X/Twitter nao retornou os dados da conta autenticada.");

            return me.Data;
        }

        private async Task<OAuth2TokenResponse> RequestOAuth2TokenAsync(IReadOnlyDictionary<string, string> form)
        {
            var (_, clientSecret) = await GetApplicationCredentialsAsync();
            var clientId = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_CLIENT_ID,
                "Authentication:Twitter:ClientId");

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            request.Headers.Authorization = CreateBasicAuthorizationHeader(clientId, clientSecret);
            request.Content = new FormUrlEncodedContent(form);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Falha ao solicitar token OAuth 2.0 no X/Twitter: {content}");

            var tokenResponse = JsonSerializer.Deserialize<OAuth2TokenResponse>(content);
            return tokenResponse ?? new OAuth2TokenResponse();
        }

        private async Task PersistOAuth2TokenAsync(OAuth2TokenResponse tokenResponse, DateTimeOffset? expiresAtUtc)
        {
            await UpsertSettingAsync(ApplicationParameter.TWITTER_ACCESS_TOKEN, tokenResponse.AccessToken, EFieldType.Password);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_ACCESS_SECRET, string.Empty, EFieldType.Password);
            await UpsertSettingAsync(ApplicationParameter.TWITTER_OAUTH_MODE, OAuth2Mode, EFieldType.Computed);

            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
                await UpsertSettingAsync(ApplicationParameter.TWITTER_REFRESH_TOKEN, tokenResponse.RefreshToken, EFieldType.Password);

            if (!string.IsNullOrWhiteSpace(tokenResponse.Scope))
                await UpsertSettingAsync(ApplicationParameter.TWITTER_TOKEN_SCOPE, tokenResponse.Scope, EFieldType.Computed);

            if (expiresAtUtc.HasValue)
                await UpsertSettingAsync(ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC, expiresAtUtc.Value.ToString("O"), EFieldType.Computed);
        }

        private async Task<(string ClientId, string ClientSecret)> GetApplicationCredentialsAsync()
        {
            var clientId = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_CLIENT_ID,
                "Authentication:Twitter:ClientId");
            var clientSecret = await GetSettingOrConfigAsync(
                ApplicationParameter.TWITTER_CLIENT_SECRET,
                "Authentication:Twitter:ClientSecret");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Configure Twitter OAuth 2.0 Client ID e Client Secret antes de autenticar o bot.");

            return (clientId.Trim(), clientSecret.Trim());
        }

        private async Task<string> GetOAuth2ScopesAsync()
        {
            var scopes = await GetStoredOrDefaultSettingValueAsync(ApplicationParameter.TWITTER_OAUTH2_SCOPES);
            return string.IsNullOrWhiteSpace(scopes)
                ? ApplicationSettingsService.DefaultTwitterOAuth2Scopes
                : string.Join(' ', scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private async Task<string> GetSettingOrConfigAsync(ApplicationParameter parameter, params string[] appSettingsKeys)
        {
            var fromSettings = await _settings.GetValueAsync(parameter);
            if (!string.IsNullOrWhiteSpace(fromSettings))
                return fromSettings;

            var fromDatabase = await GetStoredSettingValueAsync(parameter);
            if (!string.IsNullOrWhiteSpace(fromDatabase))
                return fromDatabase;

            foreach (var key in appSettingsKeys)
            {
                var fromConfig = _configuration[key];
                if (!string.IsNullOrWhiteSpace(fromConfig))
                    return fromConfig;
            }

            return string.Empty;
        }

        private async Task<string> GetStoredOrDefaultSettingValueAsync(ApplicationParameter parameter)
        {
            var stored = await GetStoredSettingValueAsync(parameter);
            if (!string.IsNullOrWhiteSpace(stored))
                return stored;

            return await _settings.GetValueAsync(parameter) ?? string.Empty;
        }

        private async Task<string> GetStoredSettingValueAsync(ApplicationParameter parameter)
        {
            var setting = await _dbContext.ApplicationSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parameter.Value);

            return setting?.Value ?? string.Empty;
        }

        private static bool ParseYesNo(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task UpsertSettingAsync(ApplicationParameter parameter, string value, EFieldType type)
        {
            var setting = await _dbContext.ApplicationSettings
                .FirstOrDefaultAsync(x => x.Id == parameter.Value);

            if (setting is null)
            {
                await _dbContext.ApplicationSettings.AddAsync(new ApplicationSettings
                {
                    Parameter = parameter,
                    Type = type,
                    Value = value ?? string.Empty
                });

                return;
            }

            setting.Type = type;
            setting.Value = value ?? string.Empty;
            _dbContext.ApplicationSettings.Update(setting);
        }

        private static DateTimeOffset? CalculateExpiresAtUtc(int expiresInSeconds)
        {
            return expiresInSeconds > 0
                ? DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
                : null;
        }

        private static AuthenticationHeaderValue CreateBasicAuthorizationHeader(string clientId, string clientSecret)
        {
            var raw = Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));
        }

        private static string CreateCodeChallenge(string codeVerifier)
        {
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            return WebEncoders.Base64UrlEncode(hash);
        }

        private static string CreateBase64UrlSecret(int byteLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        public sealed class TwitterBotAuthenticationStatus
        {
            public bool HasApplicationCredentials { get; set; }
            public bool IsAuthenticated { get; set; }
            public bool IsInternallyAuthorized { get; set; }
            public bool HasRefreshToken { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public DateTimeOffset? AuthorizedAtUtc { get; set; }
            public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }
        }

        public sealed class TwitterBotAuthenticationResult
        {
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public DateTimeOffset AuthorizedAtUtc { get; set; }
        }

        private sealed class OAuth2TokenResponse
        {
            [JsonPropertyName("token_type")]
            public string TokenType { get; set; } = string.Empty;

            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; } = string.Empty;
        }

        private sealed class OAuth2MeResponse
        {
            [JsonPropertyName("data")]
            public OAuth2UserData Data { get; set; }
        }

        private sealed class OAuth2UserData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("username")]
            public string Username { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
