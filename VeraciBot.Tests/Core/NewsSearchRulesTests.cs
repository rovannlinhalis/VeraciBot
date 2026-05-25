using FluentAssertions;
using VeraciBot.Application.External;

namespace VeraciBot.Tests.Core
{
    public class NewsSearchRulesTests
    {
        [Theory]
        [InlineData("@bot !avaliar https://example.com materia")]
        [InlineData("isso e verdade?")]
        [InlineData("pode checar essa noticia")]
        public void ShouldForceNewsSearch_ShouldDetectFactCheckRequests(string text)
        {
            NewsSearchRules.ShouldForceNewsSearch(text).Should().BeTrue();
        }

        [Fact]
        public void ShouldForceNewsSearch_ShouldUseThreadContext()
        {
            NewsSearchRules.ShouldForceNewsSearch("@bot o que acha?", "essa noticia e falsa?")
                .Should().BeTrue();
        }

        [Fact]
        public void BuildNewsSearchQuery_ShouldPreferThreadContentOverCommandMention()
        {
            var tweets = new[]
            {
                new NewsSearchTweet("bot", "@bot !avaliar"),
                new NewsSearchTweet("author-a", "Governo anuncia nova medida economica hoje")
            };

            var query = NewsSearchRules.BuildNewsSearchQuery("@bot !avaliar", tweets, "bot");

            query.Should().Be("Governo anuncia nova medida economica hoje");
        }

        [Fact]
        public void CleanNewsSearchCandidate_ShouldRemoveCommandsUrlsAndMentions()
        {
            var query = NewsSearchRules.CleanNewsSearchCandidate("@bot !checar https://example.com Isso procede?");

            query.Should().Be("Isso");
        }

        [Fact]
        public void TruncateForPrompt_ShouldKeepConfiguredLimit()
        {
            var value = new string('a', 230);

            var result = NewsSearchRules.TruncateForPrompt(value, 220);

            result.Should().HaveLength(220);
            result.Should().EndWith("...");
        }
    }
}
