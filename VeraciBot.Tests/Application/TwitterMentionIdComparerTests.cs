using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Application
{
    public class TwitterMentionIdComparerTests
    {
        [Theory]
        [InlineData("10", "2", true)]
        [InlineData("2", "10", false)]
        [InlineData("18446744073709551615", "18446744073709551614", true)]
        public void IsGreater_ShouldCompareNumericIdsAsUnsignedLongs(
            string candidateId,
            string currentId,
            bool expected)
        {
            TwitterMentionIdComparer.IsGreater(candidateId, currentId)
                .Should()
                .Be(expected);
        }

        [Fact]
        public void IsGreater_ShouldAcceptAnyCandidateWhenCurrentIdIsEmpty()
        {
            TwitterMentionIdComparer.IsGreater("123", "")
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsGreater_ShouldRejectEmptyCandidate(string candidateId)
        {
            TwitterMentionIdComparer.IsGreater(candidateId, "123")
                .Should()
                .BeFalse();
        }

        [Fact]
        public void IsGreater_ShouldFallbackToOrdinalComparisonForNonNumericIds()
        {
            TwitterMentionIdComparer.IsGreater("tweet-b", "tweet-a")
                .Should()
                .BeTrue();
        }
    }
}
