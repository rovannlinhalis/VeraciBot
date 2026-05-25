using Microsoft.Extensions.AI;

namespace VeraciBot.Application.Services
{
    public static class OpenAiModelParameterSupport
    {
        internal static OpenAiChatParameterSupport Resolve(string model)
        {
            var support = OpenAiModelParameterRules.Resolve(model);

            return new OpenAiChatParameterSupport(
                support.SupportsTemperature,
                support.SupportsMaxOutputTokens);
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

    }

    public sealed record OpenAiChatParameterSupport(
        bool SupportsTemperature,
        bool SupportsMaxOutputTokens)
    {
        public static OpenAiChatParameterSupport Default { get; } = new(
            SupportsTemperature: true,
            SupportsMaxOutputTokens: true);
    }
}
