using System.Text.Json;

namespace VeraciBot.Application.Services
{
    public sealed record AgentStructuredResponse(string Result, string Text);

    public static class AgentResponseRules
    {
        public static string ExtractFinalResponseText(
            string structuredText,
            string rawText,
            bool forceNewsSearch,
            string mentionText)
        {
            var trimmedStructuredText = structuredText?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedStructuredText)
                && !LooksLikeContextEcho(trimmedStructuredText, mentionText))
            {
                return trimmedStructuredText;
            }

            var fallbackText = rawText?.Trim();
            if (!forceNewsSearch
                && !string.IsNullOrWhiteSpace(fallbackText)
                && !LooksLikeContextEcho(fallbackText, mentionText))
            {
                return fallbackText;
            }

            return string.Empty;
        }

        public static AgentStructuredResponse TryParseAgentResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var normalized = text.Trim();
            if (normalized.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLineEnd = normalized.IndexOf('\n');
                if (firstLineEnd >= 0)
                    normalized = normalized[(firstLineEnd + 1)..];

                var fenceIndex = normalized.LastIndexOf("```", StringComparison.Ordinal);
                if (fenceIndex >= 0)
                    normalized = normalized[..fenceIndex];

                normalized = normalized.Trim();
            }

            try
            {
                using var doc = JsonDocument.Parse(normalized);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                return new AgentStructuredResponse(
                    ReadStringProperty(doc.RootElement, "result"),
                    ReadStringProperty(doc.RootElement, "text"));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool LooksLikeContextEcho(string text, string mentionText)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var markers = new[]
            {
                "RESULTADO DA PESQUISA EXTERNA",
                "Dados de fontes externas",
                "Noticias encontradas",
                "Conteudo extraido:",
                "Sites confiaveis consultados",
                "Google News RSS",
                "PESQUISA EXTERNA OBRIGATORIA",
                "PROTOCOLO DE RESPOSTA FINAL",
                "CONTEXTO DA THREAD",
                "Link da materia:"
            };

            if (markers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                return true;

            return text.Length > 500
                && !string.IsNullOrWhiteSpace(mentionText)
                && text.Contains(mentionText.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadStringProperty(JsonElement element, string propertyName)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.GetRawText();
            }

            return string.Empty;
        }
    }
}
