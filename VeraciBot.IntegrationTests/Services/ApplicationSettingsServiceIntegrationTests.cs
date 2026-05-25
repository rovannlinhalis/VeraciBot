using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.Application.Services;
using VeraciBot.Core.Entities;
using VeraciBot.Core.Enums;
using VeraciBot.IntegrationTests.Support;

namespace VeraciBot.IntegrationTests.Services
{
    public class ApplicationSettingsServiceIntegrationTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetValueAsync_ShouldReturnDefaultValueThenPersistedOverride()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();

            var defaultValue = await service.GetValueAsync(ApplicationParameter.SMTP_PORT);
            await service.UpdateAsync(
            [
                new ApplicationSettings
                {
                    Parameter = ApplicationParameter.SMTP_PORT,
                    Type = EFieldType.Number,
                    Value = "2525"
                }
            ]);
            var persistedValue = await service.GetValueAsync(ApplicationParameter.SMTP_PORT);

            defaultValue.Should().Be("587");
            persistedValue.Should().Be("2525");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTwitterMentionsWorkerSettingsAsync_ShouldParseTrimAndClampValues()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            await service.UpdateAsync(
            [
                Setting(ApplicationParameter.TWITTER_WORKER_ENABLED, EFieldType.YesNo, "false"),
                Setting(ApplicationParameter.TWITTER_WORKER_POLL_INTERVAL_SECONDS, EFieldType.Number, "2"),
                Setting(ApplicationParameter.TWITTER_WORKER_INITIAL_LOOKBACK_MINUTES, EFieldType.Number, "0"),
                Setting(ApplicationParameter.TWITTER_WORKER_MAX_RESULTS, EFieldType.Number, "999"),
                Setting(ApplicationParameter.TWITTER_WORKER_START_TIME_UTC, EFieldType.SmallText, "2026-05-24T12:00:00-03:00"),
                Setting(ApplicationParameter.TWITTER_WORKER_CURSOR_ADVANCE_SECONDS, EFieldType.Number, "999"),
                Setting(ApplicationParameter.TWITTER_WORKER_EMPTY_LOOKBACK_SECONDS, EFieldType.Number, "-5"),
                Setting(ApplicationParameter.TWITTER_USER_ID, EFieldType.SmallText, "  bot-123  ")
            ]);

            var settings = await service.GetTwitterMentionsWorkerSettingsAsync();

            settings.Enabled.Should().BeFalse();
            settings.PollIntervalSeconds.Should().Be(10);
            settings.InitialLookbackMinutes.Should().Be(1);
            settings.MaxResults.Should().Be(100);
            settings.StartTimeUtc.Should().Be(DateTimeOffset.Parse("2026-05-24T15:00:00+00:00"));
            settings.CursorAdvanceSeconds.Should().Be(60);
            settings.EmptyLookbackSeconds.Should().Be(0);
            settings.UserId.Should().Be("bot-123");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAgentSettingsAsync_ShouldClampScoresAndProcessorLimits()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            await service.UpdateAsync(
            [
                Setting(ApplicationParameter.AGENT_PROCESSOR_ENABLED, EFieldType.YesNo, "0"),
                Setting(ApplicationParameter.AGENT_PROCESSOR_IDLE_DELAY_SECONDS, EFieldType.Number, "999"),
                Setting(ApplicationParameter.OPENAI_API_KEY, EFieldType.Password, "  sk-test  "),
                Setting(ApplicationParameter.OPENAI_MODEL, EFieldType.SmallText, "  gpt-5-mini  "),
                Setting(ApplicationParameter.OPENAI_TEMPERATURE, EFieldType.Number, "-1"),
                Setting(ApplicationParameter.OPENAI_MAX_OUTPUT_TOKENS, EFieldType.Number, "999999"),
                Setting(ApplicationParameter.AGENT_SCORE_WIN_POINTS, EFieldType.Number, "9999"),
                Setting(ApplicationParameter.AGENT_SCORE_LOSS_POINTS, EFieldType.Number, "-9999"),
                Setting(ApplicationParameter.AGENT_SCORE_DRAW_POINTS, EFieldType.Number, "5")
            ]);

            var processor = await service.GetAgentProcessorSettingsAsync();
            var score = await service.GetAgentScoreSettingsAsync();

            processor.Enabled.Should().BeFalse();
            processor.IdleDelaySeconds.Should().Be(300);
            processor.OpenAiApiKey.Should().Be("sk-test");
            processor.OpenAiModel.Should().Be("gpt-5-mini");
            processor.OpenAiTemperature.Should().Be(0f);
            processor.OpenAiMaxOutputTokens.Should().Be(32000);
            score.WinPoints.Should().Be(1000);
            score.LossPoints.Should().Be(-1000);
            score.DrawPoints.Should().Be(5);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAgentSystemPromptSettingsAsync_ShouldMergeStoredOverridesWithDefaults()
        {
            await using var host = IntegrationTestHost.Create();
            await host.EnsureDatabaseCreatedAsync();

            using var scope = host.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            await service.UpdateAsync(
            [
                Setting(ApplicationParameter.AGENT_SYSTEM_IDENTITY_PROMPT, EFieldType.MultilineText, "prompt customizado")
            ]);

            var prompts = await service.GetAgentSystemPromptSettingsAsync();

            prompts.IdentityPrompt.Should().Be("prompt customizado");
            prompts.ResponseRulesPrompt.Should().Be(ApplicationSettingsService.DefaultAgentSystemResponseRulesPrompt);
            prompts.FallbackPrompt.Should().Be(ApplicationSettingsService.DefaultAgentSystemFallbackPrompt);
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
