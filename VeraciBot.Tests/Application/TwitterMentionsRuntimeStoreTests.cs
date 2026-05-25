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

        private static TwitterMentionQueueItem CreateMention(string id)
        {
            return new TwitterMentionQueueItem(
                Id: id,
                AuthorId: "author",
                Text: "text",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                EnqueuedAtUtc: DateTimeOffset.UtcNow);
        }
    }
}
