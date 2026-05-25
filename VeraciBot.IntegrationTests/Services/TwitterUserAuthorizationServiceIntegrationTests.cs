using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VeraciBot.App.Data;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;
using VeraciBot.IntegrationTests.Support;

namespace VeraciBot.IntegrationTests.Services
{
    public class TwitterUserAuthorizationServiceIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task SetAuthorizationAsync_ShouldCreateUserTrackHistoryAndAvoidDuplicateHistoryForSameStatus()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(dbContext);

            var authorized = await service.SetAuthorizationAsync(
                authorId: "author-1",
                username: "@UserOne",
                name: "User One",
                status: AuthorizedTwitterUser.STATUS_AUTHORIZED,
                changedByAuthorId: "admin-author",
                applicationUserId: 10,
                changedByApplicationUserId: 20,
                reason: "primeira autorizacao");
            var firstReturnedStatus = authorized.Status;

            await service.SetAuthorizationAsync(
                authorId: "author-1",
                username: "UserOneUpdated",
                name: "User One Updated",
                status: AuthorizedTwitterUser.STATUS_AUTHORIZED,
                reason: "sem mudanca de status");

            await service.SetAuthorizationAsync(
                authorId: "author-1",
                username: "UserOneUpdated",
                name: "User One Updated",
                status: AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED,
                reason: "bloqueio");

            var stored = await dbContext.AuthorizedTwitterUsers.SingleAsync(x => x.AuthorId == "author-1");
            var history = await dbContext.AuthorizedTwitterUserHistory
                .Where(x => x.AuthorId == "author-1")
                .OrderBy(x => x.Id)
                .ToListAsync();

            firstReturnedStatus.Should().Be(AuthorizedTwitterUser.STATUS_AUTHORIZED);
            stored.Username.Should().Be("UserOneUpdated");
            stored.Name.Should().Be("User One Updated");
            stored.Status.Should().Be(AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED);
            stored.DeauthorizationDate.Should().NotBeNull();
            history.Should().HaveCount(2);
            history[0].PreviousStatus.Should().BeEmpty();
            history[0].Status.Should().Be(AuthorizedTwitterUser.STATUS_AUTHORIZED);
            history[0].ApplicationUserId.Should().Be(10);
            history[0].ChangedByApplicationUserId.Should().Be(20);
            history[1].Status.Should().Be(AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED);
            history[1].Reason.Should().Be("bloqueio");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AddInviteCreditsAsync_ShouldCreateAuthorizedUserAndCreditTransaction()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(dbContext);
            var user = new ApplicationUser
            {
                Id = 50,
                AuthorId = "author-credits",
                TwitterUsername = "@CreditUser"
            };

            var first = await service.AddInviteCreditsAsync(user, 3, changedByApplicationUserId: 99, reason: "bonus");
            var second = await service.AddInviteCreditsAsync(user, 2, changedByApplicationUserId: 99, reason: "bonus extra");

            var stored = await dbContext.AuthorizedTwitterUsers.SingleAsync(x => x.AuthorId == "author-credits");
            var transactions = await dbContext.TwitterInviteCreditTransactions
                .Where(x => x.AuthorId == "author-credits")
                .OrderBy(x => x.Id)
                .ToListAsync();

            first.Succeeded.Should().BeTrue();
            first.BalanceAfter.Should().Be(3);
            second.Succeeded.Should().BeTrue();
            second.BalanceAfter.Should().Be(5);
            stored.InviteCredits.Should().Be(5);
            stored.Username.Should().Be("CreditUser");
            transactions.Should().HaveCount(2);
            transactions[0].Delta.Should().Be(3);
            transactions[0].BalanceAfter.Should().Be(3);
            transactions[1].Delta.Should().Be(2);
            transactions[1].BalanceAfter.Should().Be(5);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AddInviteCreditsAsync_ShouldRejectInvalidUserOrAmount()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(dbContext);

            var nullUser = await service.AddInviteCreditsAsync(null, 1, null, "teste");
            var invalidAmount = await service.AddInviteCreditsAsync(new ApplicationUser { AuthorId = "a" }, 0, null, "teste");
            var missingIdentity = await service.AddInviteCreditsAsync(new ApplicationUser(), 1, null, "teste");

            nullUser.Succeeded.Should().BeFalse();
            invalidAmount.Succeeded.Should().BeFalse();
            missingIdentity.Succeeded.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task SetApplicationUserAuthorizationAsync_ShouldPersistAuthorizationWhenUserHasAuthorId()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var service = CreateService(dbContext);
            var user = new ApplicationUser
            {
                Id = 70,
                AuthorId = "author-app-user",
                TwitterUsername = "@AppUser"
            };

            var result = await service.SetApplicationUserAuthorizationAsync(
                user,
                AuthorizedTwitterUser.STATUS_AUTHORIZED,
                changedByApplicationUserId: 1,
                reason: "admin");

            var stored = await dbContext.AuthorizedTwitterUsers.SingleAsync(x => x.AuthorId == "author-app-user");

            result.Succeeded.Should().BeTrue();
            stored.Status.Should().Be(AuthorizedTwitterUser.STATUS_AUTHORIZED);
            stored.Username.Should().Be("AppUser");
        }

        private static TwitterUserAuthorizationService CreateService(ApplicationDbContext dbContext)
        {
            return new TwitterUserAuthorizationService(
                dbContext,
                twitterApi: null,
                NullLogger<TwitterUserAuthorizationService>.Instance);
        }
    }
}
