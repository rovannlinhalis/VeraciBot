using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Application
{
    public class TwitterMentionsRuntimeStoreTests
    {
        [Fact]
        public void TryEnqueueMention_ShouldRejectDuplicateIds()
        {
            var store = new TwitterMentionsRuntimeStore();
            var item = CreateMention("100");

            store.TryEnqueueMention(item).Should().BeTrue();
            store.TryEnqueueMention(item).Should().BeFalse();

            store.GetSnapshot().Queue.Should().ContainSingle();
        }

        [Fact]
        public void MarkProcessingCompleted_ShouldUpdateStatsAndResultCounts()
        {
            var store = new TwitterMentionsRuntimeStore();
            var item = CreateMention("101");

            store.TryEnqueueMention(item);
            store.TryDequeue(out var dequeued).Should().BeTrue();
            store.MarkProcessingStarted(dequeued);
            store.MarkProcessingCompleted(dequeued, "HELP");

            var snapshot = store.GetSnapshot();
            snapshot.Stats.TotalProcessed.Should().Be(1);
            snapshot.Stats.HelpRequests.Should().Be(1);
            snapshot.CurrentMentionId.Should().BeEmpty();
        }

        [Fact]
        public void TryEnqueueMention_ShouldTrimOldestItemsAndAllowDroppedIdsAgain()
        {
            var store = new TwitterMentionsRuntimeStore(maxQueueSize: 100);
            var now = DateTimeOffset.UtcNow;

            for (var i = 0; i < 101; i++)
                store.TryEnqueueMention(CreateMention(i.ToString(), now.AddSeconds(i))).Should().BeTrue();

            var snapshot = store.GetSnapshot();
            snapshot.Queue.Should().HaveCount(100);
            snapshot.Queue.Select(x => x.Id).Should().NotContain("0");

            store.TryEnqueueMention(CreateMention("0", now.AddSeconds(200))).Should().BeTrue();

            snapshot = store.GetSnapshot();
            snapshot.Queue.Should().HaveCount(100);
            snapshot.Queue.Select(x => x.Id).Should().Contain("0");
        }

        [Fact]
        public void AddLog_ShouldTrimOldestEntriesAndKeepSourceSpecificViews()
        {
            var store = new TwitterMentionsRuntimeStore(maxLogEntries: 100);

            for (var i = 0; i < 105; i++)
            {
                if (i % 2 == 0)
                    store.AddTwitterLog("info", $"twitter-{i}");
                else
                    store.AddAgentLog("warning", $"agent-{i}");
            }

            var snapshot = store.GetSnapshot();
            snapshot.Logs.Should().HaveCount(100);
            snapshot.Logs.Select(x => x.Message).Should().NotContain("twitter-0");
            snapshot.Logs.Select(x => x.Message).Should().Contain("twitter-104");
            snapshot.TwitterLogs.Should().OnlyContain(x => x.Source == "Twitter");
            snapshot.AgentLogs.Should().OnlyContain(x => x.Source == "Agent");
        }

        [Fact]
        public void MarkProcessingSkippedAndFailed_ShouldUpdateStatsAndClearCurrentMention()
        {
            var store = new TwitterMentionsRuntimeStore();
            var skipped = CreateMention("201");
            var failed = CreateMention("202");

            store.MarkProcessingStarted(skipped);
            store.MarkProcessingSkipped(skipped, "not_authorized");
            store.MarkProcessingStarted(failed);
            store.MarkProcessingFailed(failed, new InvalidOperationException("erro controlado"));

            var snapshot = store.GetSnapshot();
            snapshot.Stats.TotalSkipped.Should().Be(1);
            snapshot.Stats.UnauthorizedMentions.Should().Be(1);
            snapshot.Stats.TotalErrors.Should().Be(1);
            snapshot.LastProcessorError.Should().Be("erro controlado");
            snapshot.CurrentMentionId.Should().BeEmpty();
            snapshot.LastProcessingStartedAtUtc.Should().BeNull();
        }

        [Theory]
        [InlineData("THREAD_ARGUE_1", 1, 0, 0)]
        [InlineData("THREAD_ARGUE_2", 0, 1, 0)]
        [InlineData("THREAD_ARGUE_0", 0, 0, 1)]
        public void MarkProcessingCompleted_ShouldMapDebateResultStats(
            string result,
            int authorAWins,
            int authorBWins,
            int draws)
        {
            var store = new TwitterMentionsRuntimeStore();
            var item = CreateMention("301");

            store.MarkProcessingCompleted(item, result);

            var stats = store.GetSnapshot().Stats;
            stats.ThreadArgumentAnalyses.Should().Be(1);
            stats.DebateAuthorAWins.Should().Be(authorAWins);
            stats.DebateAuthorBWins.Should().Be(authorBWins);
            stats.DebateDraws.Should().Be(draws);
        }

        private static TwitterMentionQueueItem CreateMention(string id, DateTimeOffset? enqueuedAtUtc = null)
        {
            return new TwitterMentionQueueItem(
                Id: id,
                AuthorId: "author",
                Text: "text",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                EnqueuedAtUtc: enqueuedAtUtc ?? DateTimeOffset.UtcNow);
        }
    }
}
