using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.App.Data;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;
using VeraciBot.Core.Enums;
using VeraciBot.Core.Interfaces;
using VeraciBot.IntegrationTests.Support;

namespace VeraciBot.IntegrationTests.Infrastructure
{
    public class ApplicationDbContextIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task DbContext_ShouldPersistProcessedMentionWithLlmHistoryAcrossScopes()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                dbContext.ProcessedMentions.Add(new ProcessedMention
                {
                    TweetId = "tweet-100",
                    AuthorId = "author-1",
                    MentionUrl = "https://x.com/user/status/tweet-100",
                    Text = "@bot avalie",
                    Result = "THREAD_FACT_TRUE",
                    LlmRequests =
                    [
                        new LlmRequestHistory
                        {
                            Model = "gpt-4o-mini",
                            LlmResult = "THREAD_FACT_TRUE",
                            Success = true,
                            TotalTokens = 42
                        }
                    ]
                });

                await dbContext.SaveChangesAsync();
            }

            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var mention = await dbContext.ProcessedMentions
                    .Include(x => x.LlmRequests)
                    .SingleAsync(x => x.TweetId == "tweet-100");

                mention.AuthorId.Should().Be("author-1");
                mention.Result.Should().Be("THREAD_FACT_TRUE");
                mention.LlmRequests.Should().ContainSingle()
                    .Which.TotalTokens.Should().Be(42);
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ApplicationSettingsService_ShouldResolveFromDiPersistOverridesAndUseBlobStorage()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var settingsService = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();

            await blobStorage.UploadFileAsync(
                "logo.jpg",
                new MemoryStream([1, 2, 3]),
                "image/jpeg");

            await settingsService.UpdateAsync(
            [
                new ApplicationSettings
                {
                    Parameter = ApplicationParameter.TWITTER_WORKER_MAX_RESULTS,
                    Type = EFieldType.Number,
                    Value = "999"
                },
                new ApplicationSettings
                {
                    Parameter = ApplicationParameter.OPENAI_MODEL,
                    Type = EFieldType.SmallText,
                    Value = " gpt-5-mini "
                },
                new ApplicationSettings
                {
                    Parameter = ApplicationParameter.OPENAI_TEMPERATURE,
                    Type = EFieldType.Number,
                    Value = "8"
                }
            ]);

            var workerSettings = await settingsService.GetTwitterMentionsWorkerSettingsAsync();
            var agentSettings = await settingsService.GetAgentProcessorSettingsAsync();
            var allSettings = (await settingsService.GetAllAsync()).ToList();

            workerSettings.MaxResults.Should().Be(100);
            agentSettings.OpenAiModel.Should().Be("gpt-5-mini");
            agentSettings.OpenAiTemperature.Should().Be(2f);
            allSettings.Single(x => x.Id == ApplicationParameter.HELP_IMAGE.Value)
                .FileUrl.Should()
                .Be("/assets/logo.jpg");
        }
    }
}
