using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Core
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
    }
}
