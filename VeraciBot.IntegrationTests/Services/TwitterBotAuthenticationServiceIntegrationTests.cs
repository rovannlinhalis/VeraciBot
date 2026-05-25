using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.App.Data;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;
using VeraciBot.Core.Enums;
using VeraciBot.IntegrationTests.Support;

namespace VeraciBot.IntegrationTests.Services
{
    public class TwitterBotAuthenticationServiceIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetStatusAsync_ShouldReadCredentialsFromConfigurationAndStoredBotSettings()
        {
            await using var host = IntegrationTestHost.Create(new Dictionary<string, string>
            {
                ["Authentication:Twitter:ClientId"] = "client-id",
                ["Authentication:Twitter:ClientSecret"] = "client-secret"
            });
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            var auth = scope.ServiceProvider.GetRequiredService<TwitterBotAuthenticationService>();
            var expiresAt = DateTimeOffset.UtcNow.AddHours(1).ToString("O");
            var authorizedAt = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O");
            await settings.UpdateAsync(
            [
                Setting(ApplicationParameter.TWITTER_ACCESS_TOKEN, EFieldType.Password, "access-token"),
                Setting(ApplicationParameter.TWITTER_REFRESH_TOKEN, EFieldType.Password, "refresh-token"),
                Setting(ApplicationParameter.TWITTER_USER_ID, EFieldType.SmallText, "bot-id"),
                Setting(ApplicationParameter.TWITTER_BOT_USERNAME, EFieldType.Computed, "veracibot"),
                Setting(ApplicationParameter.TWITTER_BOT_NAME, EFieldType.Computed, "Veraci Bot"),
                Setting(ApplicationParameter.TWITTER_BOT_AUTHORIZED_AT_UTC, EFieldType.Computed, authorizedAt),
                Setting(ApplicationParameter.TWITTER_BOT_AUTHORIZED, EFieldType.Computed, "1"),
                Setting(ApplicationParameter.TWITTER_OAUTH_MODE, EFieldType.Computed, "OAuth2"),
                Setting(ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC, EFieldType.Computed, expiresAt)
            ]);

            var status = await auth.GetStatusAsync();

            status.HasApplicationCredentials.Should().BeTrue();
            status.IsAuthenticated.Should().BeTrue();
            status.IsInternallyAuthorized.Should().BeTrue();
            status.HasRefreshToken.Should().BeTrue();
            status.UserId.Should().Be("bot-id");
            status.Username.Should().Be("veracibot");
            status.Name.Should().Be("Veraci Bot");
            status.AuthorizedAtUtc.Should().NotBeNull();
            status.AccessTokenExpiresAtUtc.Should().NotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task BeginAuthorizationAsync_ShouldPersistStateCodeVerifierAndReturnOAuthUrl()
        {
            await using var host = IntegrationTestHost.Create(new Dictionary<string, string>
            {
                ["Authentication:Twitter:ClientId"] = "client-id",
                ["Authentication:Twitter:ClientSecret"] = "client-secret"
            });
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            var auth = scope.ServiceProvider.GetRequiredService<TwitterBotAuthenticationService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await settings.UpdateAsync(
            [
                Setting(ApplicationParameter.TWITTER_OAUTH2_SCOPES, EFieldType.SmallText, " tweet.read   users.read ")
            ]);

            var authorizationUrl = await auth.BeginAuthorizationAsync("https://app.local/twitter/callback");

            var uri = new Uri(authorizationUrl);
            var query = QueryHelpers.ParseQuery(uri.Query);
            var state = await dbContext.ApplicationSettings
                .AsNoTracking()
                .SingleAsync(x => x.Id == ApplicationParameter.TWITTER_OAUTH2_STATE.Value);
            var codeVerifier = await dbContext.ApplicationSettings
                .AsNoTracking()
                .SingleAsync(x => x.Id == ApplicationParameter.TWITTER_OAUTH2_CODE_VERIFIER.Value);

            uri.GetLeftPart(UriPartial.Path).Should().Be("https://x.com/i/oauth2/authorize");
            query["response_type"].ToString().Should().Be("code");
            query["client_id"].ToString().Should().Be("client-id");
            query["redirect_uri"].ToString().Should().Be("https://app.local/twitter/callback");
            query["scope"].ToString().Should().Be("tweet.read users.read");
            query["code_challenge_method"].ToString().Should().Be("S256");
            query["state"].ToString().Should().Be(state.Value);
            query["code_challenge"].ToString().Should().NotBeNullOrWhiteSpace();
            state.Value.Should().NotBeNullOrWhiteSpace();
            codeVerifier.Value.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task BeginAuthorizationAsync_ShouldRejectMissingCallbackOrCredentials()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<TwitterBotAuthenticationService>();

            await auth.Awaiting(x => x.BeginAuthorizationAsync(""))
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*Callback OAuth invalido*");

            await auth.Awaiting(x => x.BeginAuthorizationAsync("https://app.local/callback"))
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*Client ID e Client Secret*");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetOAuth2AccessTokenAsync_ShouldReturnStoredTokenWhenNotNearExpiry()
        {
            await using var host = IntegrationTestHost.Create(new Dictionary<string, string>
            {
                ["Authentication:Twitter:ClientId"] = "client-id",
                ["Authentication:Twitter:ClientSecret"] = "client-secret"
            });
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            var auth = scope.ServiceProvider.GetRequiredService<TwitterBotAuthenticationService>();
            await settings.UpdateAsync(
            [
                Setting(ApplicationParameter.TWITTER_ACCESS_TOKEN, EFieldType.Password, "valid-token"),
                Setting(ApplicationParameter.TWITTER_REFRESH_TOKEN, EFieldType.Password, "refresh-token"),
                Setting(ApplicationParameter.TWITTER_TOKEN_EXPIRES_AT_UTC, EFieldType.Computed, DateTimeOffset.UtcNow.AddHours(1).ToString("O"))
            ]);

            var token = await auth.GetOAuth2AccessTokenAsync();

            token.Should().Be("valid-token");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetOAuth2AccessTokenAsync_ShouldThrowWhenAccessTokenIsMissing()
        {
            await using var host = IntegrationTestHost.Create(new Dictionary<string, string>
            {
                ["Authentication:Twitter:ClientId"] = "client-id",
                ["Authentication:Twitter:ClientSecret"] = "client-secret"
            });
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<TwitterBotAuthenticationService>();

            await auth.Awaiting(x => x.GetOAuth2AccessTokenAsync())
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("*access token OAuth 2.0*");
        }

        private static ApplicationSettings Setting(
            ApplicationParameter parameter,
            EFieldType type,
            string value)
        {
            return new ApplicationSettings
            {
                Parameter = parameter,
                Type = type,
                Value = value
            };
        }
    }
}
