using Microsoft.Extensions.AI;

namespace VeraciBot.App.Services
{
    internal static class OpenAiModelParameterSupport
    {
        private static readonly ModelParameterRule[] Rules =
        [
            new("gpt-5", SupportsTemperature: false, SupportsMaxOutputTokens: true),
            new("o1", SupportsTemperature: false, SupportsMaxOutputTokens: true),
            new("o3", SupportsTemperature: false, SupportsMaxOutputTokens: true),
            new("o4", SupportsTemperature: false, SupportsMaxOutputTokens: true)
        ];

        public static OpenAiChatParameterSupport Resolve(string model)
        {
            var normalizedModel = NormalizeModelName(model);
            var rule = Rules.FirstOrDefault(x => normalizedModel.StartsWith(x.ModelPrefix, StringComparison.OrdinalIgnoreCase));

            return rule is null
                ? OpenAiChatParameterSupport.Default
                : new OpenAiChatParameterSupport(rule.SupportsTemperature, rule.SupportsMaxOutputTokens);
        }

        public static ChatOptions CreateChatOptions(ApplicationSettingsService.AgentProcessorSettings settings, Action<string> onSuppressedParameter = null)
        {
            var support = Resolve(settings.OpenAiModel);
            var options = new ChatOptions();

            if (support.SupportsTemperature)
            {
                options.Temperature = settings.OpenAiTemperature;
            }
            else
            {
                onSuppressedParameter?.Invoke($"temperature nao foi enviado porque o modelo {settings.OpenAiModel} nao suporta esse parametro.");
            }

            if (support.SupportsMaxOutputTokens)
            {
                options.MaxOutputTokens = settings.OpenAiMaxOutputTokens;
            }
            else
            {
                onSuppressedParameter?.Invoke($"maxOutputTokens nao foi enviado porque o modelo {settings.OpenAiModel} nao suporta esse parametro.");
            }

            return options;
        }

        private static string NormalizeModelName(string model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return string.Empty;

            var normalized = model.Trim().ToLowerInvariant();
            var slashIndex = normalized.LastIndexOf('/');

            return slashIndex >= 0 && slashIndex < normalized.Length - 1
                ? normalized[(slashIndex + 1)..]
                : normalized;
        }

        private sealed record ModelParameterRule(
            string ModelPrefix,
            bool SupportsTemperature,
            bool SupportsMaxOutputTokens);
    }

    internal sealed record OpenAiChatParameterSupport(
        bool SupportsTemperature,
        bool SupportsMaxOutputTokens)
    {
        public static OpenAiChatParameterSupport Default { get; } = new(
            SupportsTemperature: true,
            SupportsMaxOutputTokens: true);
    }
}
