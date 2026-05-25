using FluentAssertions;
using VeraciBot.Application.External;

namespace VeraciBot.Tests.Application
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
        public void ShouldForceNewsSearch_ShouldIgnoreOrdinaryConversation()
        {
            NewsSearchRules.ShouldForceNewsSearch("@bot bom dia, como voce esta?")
                .Should()
                .BeFalse();
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
        public void BuildNewsSearchQuery_ShouldUseFallbackTextsWhenMentionAndThreadHaveNoCandidate()
        {
            var tweets = new[]
            {
                new NewsSearchTweet("bot", "@bot !avaliar")
            };
            var fallbackTexts = new[]
            {
                "",
                "@bot pode checar?",
                "Empresa confirma compra de startup brasileira"
            };

            var query = NewsSearchRules.BuildNewsSearchQuery("@bot !avaliar", tweets, "bot", fallbackTexts);

            query.Should().Be("Empresa confirma compra de startup brasileira");
        }

        [Fact]
        public void CleanNewsSearchCandidate_ShouldRemoveCommandsUrlsAndMentions()
        {
            var query = NewsSearchRules.CleanNewsSearchCandidate("@bot !checar https://example.com Isso procede?");

            query.Should().Be("Isso");
        }

        [Fact]
        public void SelectBestNewsSearchCandidate_ShouldPreferSubstantiveContentOverShortRequest()
        {
            var candidate = NewsSearchRules.SelectBestNewsSearchCandidate(
                ["verdade", "Texto longo com fato relevante para checagem"]);

            candidate.Should().Be("Texto longo com fato relevante para checagem");
        }

        [Fact]
        public void AreEquivalentSearchTexts_ShouldNormalizeWhitespaceAndCase()
        {
            NewsSearchRules.AreEquivalentSearchTexts(
                    "  Governo   anuncia MEDIDA ",
                    "governo anuncia medida")
                .Should()
                .BeTrue();
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
