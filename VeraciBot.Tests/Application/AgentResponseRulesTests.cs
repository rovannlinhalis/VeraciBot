using FluentAssertions;
using VeraciBot.Application.Services;

namespace VeraciBot.Tests.Application
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
        public void TryParseAgentResponse_ShouldReadCaseInsensitivePropertiesAndNonStringValues()
        {
            var response = AgentResponseRules.TryParseAgentResponse("""
                {"RESULT":"SCORE","TEXT":123}
                """);

            response.Should().NotBeNull();
            response.Result.Should().Be("SCORE");
            response.Text.Should().Be("123");
        }

        [Fact]
        public void TryParseAgentResponse_ShouldReturnNullForMalformedJson()
        {
            AgentResponseRules.TryParseAgentResponse("""{"result":"HELP","text":""")
                .Should()
                .BeNull();
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

        [Fact]
        public void ExtractFinalResponseText_ShouldPreferStructuredTextOverRawText()
        {
            var text = AgentResponseRules.ExtractFinalResponseText(
                structuredText: "Resposta estruturada",
                rawText: "Resposta bruta",
                forceNewsSearch: false,
                mentionText: "oi");

            text.Should().Be("Resposta estruturada");
        }

        [Fact]
        public void ExtractFinalResponseText_ShouldFallbackToRawWhenStructuredTextLooksLikeContextEcho()
        {
            var text = AgentResponseRules.ExtractFinalResponseText(
                structuredText: "CONTEXTO DA THREAD: dados internos",
                rawText: "Resposta final",
                forceNewsSearch: false,
                mentionText: "avalie");

            text.Should().Be("Resposta final");
        }
    }
}
