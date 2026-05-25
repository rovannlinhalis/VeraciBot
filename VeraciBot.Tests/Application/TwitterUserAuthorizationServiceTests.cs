using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;

namespace VeraciBot.Tests.Application
{
    public class TwitterUserAuthorizationServiceTests
    {
        [Theory]
        [InlineData("@UserName", "UserName")]
        [InlineData("  @UserName  ", "UserName")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizeUsername_ShouldTrimWhitespaceAndLeadingAt(string username, string expected)
        {
            TwitterUserAuthorizationService.NormalizeUsername(username)
                .Should()
                .Be(expected);
        }

        [Fact]
        public void ApplyExternalLoginInfo_ShouldPopulateTwitterIdentityFromClaims()
        {
            var user = new ApplicationUser();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("urn:twitter:userid", "author-123"),
                new Claim("screen_name", "@ClaimUser")
            ]));
            var loginInfo = new ExternalLoginInfo(principal, "Twitter", "provider-key", "Twitter");

            var changed = TwitterUserAuthorizationService.ApplyExternalLoginInfo(user, loginInfo);

            changed.Should().BeTrue();
            user.AuthorId.Should().Be("author-123");
            user.TwitterUsername.Should().Be("ClaimUser");
        }

        [Fact]
        public void ApplyExternalLoginInfo_ShouldUseProviderKeyWhenAuthorClaimIsMissing()
        {
            var user = new ApplicationUser();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "FallbackName")
            ]));
            var loginInfo = new ExternalLoginInfo(principal, "Twitter", "provider-key", "Twitter");

            var changed = TwitterUserAuthorizationService.ApplyExternalLoginInfo(user, loginInfo);

            changed.Should().BeTrue();
            user.AuthorId.Should().Be("provider-key");
            user.TwitterUsername.Should().Be("FallbackName");
        }

        [Fact]
        public void ApplyExternalLoginInfo_ShouldIgnoreNonTwitterProvider()
        {
            var user = new ApplicationUser();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "author-123")
            ]));
            var loginInfo = new ExternalLoginInfo(principal, "Google", "provider-key", "Google");

            var changed = TwitterUserAuthorizationService.ApplyExternalLoginInfo(user, loginInfo);

            changed.Should().BeFalse();
            user.AuthorId.Should().BeEmpty();
            user.TwitterUsername.Should().BeEmpty();
        }
    }
}
