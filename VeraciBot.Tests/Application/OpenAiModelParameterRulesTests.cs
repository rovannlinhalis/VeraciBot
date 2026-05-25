using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Application
{
    public class OpenAiModelParameterRulesTests
    {
        [Theory]
        [InlineData("gpt-5", false, true)]
        [InlineData("azure/o3-mini", false, true)]
        [InlineData("gpt-4o-mini", true, true)]
        public void Resolve_ShouldReturnKnownParameterSupport(string model, bool supportsTemperature, bool supportsMaxOutputTokens)
        {
            var support = OpenAiModelParameterRules.Resolve(model);

            support.SupportsTemperature.Should().Be(supportsTemperature);
            support.SupportsMaxOutputTokens.Should().Be(supportsMaxOutputTokens);
        }

        [Theory]
        [InlineData(" azure/openai/gpt-5-mini ", "gpt-5-mini")]
        [InlineData("GPT-4O-MINI", "gpt-4o-mini")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizeModelName_ShouldTrimProviderPrefixAndNormalizeCase(string model, string expected)
        {
            OpenAiModelParameterRules.NormalizeModelName(model)
                .Should()
                .Be(expected);
        }

        [Fact]
        public void CreateChatOptions_ShouldSuppressUnsupportedTemperatureForGpt5Models()
        {
            var suppressed = new List<string>();
            var settings = new ApplicationSettingsService.AgentProcessorSettings
            {
                OpenAiModel = "gpt-5",
                OpenAiTemperature = 0.8f,
                OpenAiMaxOutputTokens = 1200
            };

            var options = OpenAiModelParameterSupport.CreateChatOptions(settings, suppressed.Add);

            options.Temperature.Should().BeNull();
            options.MaxOutputTokens.Should().Be(1200);
            suppressed.Should().ContainSingle()
                .Which.Should().Contain("temperature");
        }
    }
}
