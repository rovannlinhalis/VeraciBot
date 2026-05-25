using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Core
{
    public class AgentResponseRulesTests
    {
        [Fact]
        public void TryParseAgentResponse_ShouldParseJsonInsideFence()
        {
            var response = AgentResponseRules.TryParseAgentResponse("""
                ```json
                {"result":"HELP","text":"Use !ajuda."}
                ```
                """);

            response.Should().NotBeNull();
            response.Result.Should().Be("HELP");
            response.Text.Should().Be("Use !ajuda.");
        }

        [Fact]
        public void ExtractFinalResponseText_ShouldRejectSearchContextEchoWhenForced()
        {
            var text = AgentResponseRules.ExtractFinalResponseText(
                structuredText: "",
                rawText: "RESULTADO DA PESQUISA EXTERNA: dados brutos",
                forceNewsSearch: true,
                mentionText: "!avaliar");

            text.Should().BeEmpty();
        }

        [Fact]
        public void ExtractFinalResponseText_ShouldUseRawTextWhenNotForced()
        {
            var text = AgentResponseRules.ExtractFinalResponseText(
                structuredText: "",
                rawText: "Resposta curta",
                forceNewsSearch: false,
                mentionText: "oi");

            text.Should().Be("Resposta curta");
        }
    }
}
