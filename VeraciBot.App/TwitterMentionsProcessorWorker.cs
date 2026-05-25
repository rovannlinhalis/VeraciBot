using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using VeraciBot.App.Data;
using VeraciBot.Core.Entities;
using VeraciBot.Application.External;

namespace VeraciBot.Application.Services
{
    public sealed class TwitterMentionsProcessorWorker : BackgroundService
    {
        private const string ThreadContextLabel = "CONTEXTO DA THREAD (debate entre os usuarios):";
        private static readonly Regex NewsSearchCommandRegex = new(@"(^|\s)!(avaliar|falso|verificar|checar)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NewsSearchRequestRegex = new(@"\b(pode\s+)?(avaliar|avalia|avalie|verificar|verifique|checar|cheque|confere|procede|isso\s+[e\u00e9]\s+(verdade|falso|fake|mentira)|essa?\s+not[i\u00ed]cia\s+[e\u00e9]\s+(verdade|falsa|fake)|verdadeiro\s+ou\s+falso)\b[?.!,;:]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NewsSearchRequestPrefixRegex = new(@"^\s*(sobre|analise|analisa|olha|veja|diz\s+se|me\s+diz\s+se|quero\s+saber\s+se|essa?\s+not[i\u00ed]cia|esta?\s+not[i\u00ed]cia|not[i\u00ed]cia)\b[?.!,;:\-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NewsSearchUrlRegex = new(@"(?:https?://|www\.)[^\s<>""]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TwitterMentionsRuntimeStore _runtimeStore;
        private readonly ILogger<TwitterMentionsProcessorWorker> _logger;

        public TwitterMentionsProcessorWorker(
            IServiceScopeFactory scopeFactory,
            TwitterMentionsRuntimeStore runtimeStore,
            ILogger<TwitterMentionsProcessorWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _runtimeStore = runtimeStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _runtimeStore.SetProcessorState(true);
            _runtimeStore.AddAgentLog("info", "Processor de mencoes iniciado.");
            var idleDelaySeconds = ApplicationSettingsService.DefaultAgentProcessorIdleDelaySeconds;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (!_runtimeStore.TryDequeue(out var mention))
                    {
                        idleDelaySeconds = await GetProcessorIdleDelaySecondsAsync(idleDelaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(idleDelaySeconds), stoppingToken);
                        continue;
                    }

                    try
                    {
                        _runtimeStore.MarkProcessingStarted(mention);
                        await ProcessMentionAsync(mention, stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "Erro ao processar mencao {TweetId}.", mention.Id);
                        _runtimeStore.MarkProcessingFailed(mention, ex);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected on shutdown.
            }
            finally
            {
                _runtimeStore.SetProcessorState(false);
                _runtimeStore.AddAgentLog("info", "Processor de mencoes finalizado.");
            }
        }

        private async Task ProcessMentionAsync(TwitterMentionQueueItem mention, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var settingsService = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
            var twitterApi = scope.ServiceProvider.GetRequiredService<TwitterAPI>();
            var agentTools = scope.ServiceProvider.GetRequiredService<VeraciBotAgentTools>();

            var processorSettings = await settingsService.GetAgentProcessorSettingsAsync();
            var runtimeSettings = await settingsService.GetTwitterMentionsRuntimeSettingsAsync();
            _runtimeStore.ConfigureLimits(runtimeSettings.MaxQueueSize, runtimeSettings.MaxLogEntries);

            if (!processorSettings.Enabled)
            {
                _runtimeStore.MarkProcessingSkipped(mention, "PROCESSOR_DISABLED");
                return;
            }

            var alreadyProcessed = await db.ProcessedMentions
                .AnyAsync(p => p.TweetId == mention.Id, stoppingToken);

            if (alreadyProcessed)
            {
                _logger.LogDebug("Mencao {TweetId} ja processada.", mention.Id);
                _runtimeStore.MarkProcessingSkipped(mention, "ALREADY_PROCESSED");
                return;
            }

            _runtimeStore.AddAgentLog("info", $"Processando mencao {mention.Id} de {mention.AuthorId}...");

            var workerSettings = await settingsService.GetTwitterMentionsWorkerSettingsAsync();
            var botUserId = workerSettings.UserId;

            var authorization = await db.AuthorizedTwitterUsers
                .FirstOrDefaultAsync(u => u.AuthorId == mention.AuthorId, stoppingToken);

            if (authorization == null || authorization.Status == AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED)
            {
                db.ProcessedMentions.Add(new ProcessedMention
                {
                    TweetId = mention.Id,
                    MentionUrl = BuildMentionUrl(mention.Id),
                    AuthorId = mention.AuthorId,
                    Text = mention.Text,
                    ProcessedAtUtc = DateTimeOffset.UtcNow,
                    Result = "NOT_AUTHORIZED"
                });
                await db.SaveChangesAsync(stoppingToken);

                var notAuthImage = await settingsService.GetValueAsync(ApplicationParameter.NO_AUTHORIZED_IMAGE);
                var notAuthMsg = await settingsService.GetValueAsync(ApplicationParameter.AGENT_NOT_AUTHORIZED_TEXT)
                    ?? "Voce nao esta autorizado a usar o VeraciBot. Peca para alguem te convidar com !convidar @seu_usuario.";

                if (!string.IsNullOrWhiteSpace(notAuthImage))
                    await twitterApi.PostReplyWithImageAsync(notAuthMsg, notAuthImage, mention.Id);
                else
                    await twitterApi.PostReplyAsync(notAuthMsg, mention.Id);

                _runtimeStore.MarkProcessingCompleted(mention, "NOT_AUTHORIZED");
                return;
            }

            if (string.IsNullOrWhiteSpace(processorSettings.OpenAiApiKey))
            {
                _runtimeStore.MarkProcessingSkipped(mention, "OPENAI_NOT_CONFIGURED");
                return;
            }

            var promptSettings = await settingsService.GetAgentSystemPromptSettingsAsync();
            var fullThread = await twitterApi.GetThreadContext(mention.Id, mention.AuthorId);
            var isSingleTweet = fullThread == null
                || fullThread.AuthorA == botUserId
                || fullThread.Tweets.Count <= 1;

            agentTools.SetContext(mention.Id, mention.AuthorId, mention.Text, botUserId, authorization, fullThread, isSingleTweet);
            var forceNewsSearch = ShouldForceNewsSearch(mention.Text, fullThread);
            var preloadedNewsContext = string.Empty;

            if (forceNewsSearch)
            {
                var searchQuery = BuildNewsSearchQuery(mention.Text, fullThread, botUserId);
                _runtimeStore.AddAgentLog("info", $"Consulta externa obrigatoria para avaliacao factual: {searchQuery}");
                var searchResult = await agentTools.SearchNewsOnGoogle(searchQuery);
                preloadedNewsContext = searchResult.Data ?? string.Empty;
            }

            var openAiClient = new OpenAIClient(processorSettings.OpenAiApiKey);
            var chatClient = openAiClient.GetChatClient(processorSettings.OpenAiModel).AsIChatClient();

            var tools = new AIFunction[]
            {
                AIFunctionFactory.Create(agentTools.RespondHelp),
                AIFunctionFactory.Create(agentTools.RespondScore),
                AIFunctionFactory.Create(agentTools.RespondScoreboard),
                AIFunctionFactory.Create(agentTools.InviteUser),
                AIFunctionFactory.Create(agentTools.AcceptInvite),
                AIFunctionFactory.Create(agentTools.RefuseInvite),
                AIFunctionFactory.Create(agentTools.RespondThreadArgue),
                AIFunctionFactory.Create(agentTools.RespondThreadFalse),
                AIFunctionFactory.Create(agentTools.RespondThreadWhoIsRight),
                AIFunctionFactory.Create(agentTools.SearchNewsOnGoogle),
                AIFunctionFactory.Create(agentTools.RespondUnknownCommand),
            };

            var agent = chatClient.AsAIAgent(
                instructions: BuildSystemPrompt(promptSettings, authorization, isSingleTweet, fullThread, forceNewsSearch, preloadedNewsContext),
                name: "VeraciBotAgent",
                tools: tools);

            var chatOptions = OpenAiModelParameterSupport.CreateChatOptions(
                processorSettings,
                message => _runtimeStore.AddAgentLog("info", message));
            var runOptions = new ChatClientAgentRunOptions(chatOptions);

            var agentResponse = await agent.RunAsync<TwitterAgentResponse>(
                BuildAgentInput(mention.Text, forceNewsSearch, preloadedNewsContext),
                options: runOptions,
                cancellationToken: stoppingToken);

            var response = agentResponse.Result ?? TryParseAgentResponse(agentResponse.Text);
            var result = !string.IsNullOrWhiteSpace(response?.Result)
                ? response.Result.Trim()
                : !string.IsNullOrWhiteSpace(agentTools.LastResult)
                    ? agentTools.LastResult
                    : "LLM_RESPONSE";
            var text = ExtractFinalResponseText(response, agentResponse.Text, forceNewsSearch, mention.Text);

            if (string.IsNullOrWhiteSpace(text))
            {
                result = !string.IsNullOrWhiteSpace(agentTools.LastResult)
                    ? agentTools.LastResult
                    : "EMPTY_LLM_RESPONSE";
                text = "Nao consegui gerar uma resposta para esta mencao.";
            }

            Exception publishException = null;
            try
            {
                await agentTools.PublishAndMarkAsync(text, result, stoppingToken);
            }
            catch (Exception ex)
            {
                publishException = ex;
                _runtimeStore.AddAgentLog("error", $"Falha ao publicar resposta da mencao {mention.Id}: {ex.Message}");
            }

            await SaveLlmRequestHistoryAsync(
                db,
                mention.Id,
                processorSettings.OpenAiModel,
                result,
                success: publishException is null,
                mentionText: mention.Text,
                responseText: text,
                consultedNewsLinks: agentTools.ConsultedNewsLinks,
                processSteps: agentTools.ProcessSteps,
                responseObject: agentResponse,
                publishError: publishException?.Message ?? string.Empty,
                forceNewsSearch: forceNewsSearch,
                cancellationToken: stoppingToken);

            if (publishException is not null)
                throw new InvalidOperationException($"Falha ao publicar resposta da mencao {mention.Id}.", publishException);

            _runtimeStore.MarkProcessingCompleted(mention, result);
        }

        private static async Task SaveLlmRequestHistoryAsync(
            ApplicationDbContext db,
            string mentionId,
            string model,
            string llmResult,
            bool success,
            string mentionText,
            string responseText,
            IReadOnlyCollection<string> consultedNewsLinks,
            IReadOnlyCollection<string> processSteps,
            object? responseObject,
            string publishError,
            bool forceNewsSearch,
            CancellationToken cancellationToken)
        {
            var usage = ExtractUsage(responseObject);
            var links = (consultedNewsLinks ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var steps = (processSteps ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            var metadata = new
            {
                mentionText,
                responseText,
                llmResult,
                timestampUtc = DateTimeOffset.UtcNow,
                usage,
                forceNewsSearch,
                publishError,
                consultedNewsLinks = links,
                processSteps = steps
            };

            db.LlmRequestHistory.Add(new LlmRequestHistory
            {
                ProcessedMentionTweetId = mentionId,
                RequestedAtUtc = DateTimeOffset.UtcNow,
                Model = string.IsNullOrWhiteSpace(model) ? "unknown" : model.Trim(),
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                TotalTokens = usage.TotalTokens,
                LlmResult = string.IsNullOrWhiteSpace(llmResult) ? "UNKNOWN" : llmResult.Trim(),
                Success = success,
                MetadataJson = JsonSerializer.Serialize(metadata),
                ConsultedNewsLinksJson = JsonSerializer.Serialize(links),
                ProcessStepsJson = JsonSerializer.Serialize(steps)
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        private static LlmUsage ExtractUsage(object? responseObject)
        {
            if (responseObject is null)
                return new LlmUsage(null, null, null);

            var usageObj = GetPropertyValue(responseObject, "Usage")
                ?? GetPropertyValue(responseObject, "TokenUsage");

            var nestedResult = GetPropertyValue(responseObject, "Result");
            usageObj ??= GetPropertyValue(nestedResult ?? new object(), "Usage");
            usageObj ??= GetPropertyValue(nestedResult ?? new object(), "TokenUsage");

            var promptTokens = ReadInt(usageObj, "PromptTokens")
                ?? ReadInt(usageObj, "InputTokens")
                ?? ReadInt(usageObj, "PromptTokenCount")
                ?? ReadInt(usageObj, "InputTokenCount")
                ?? ReadInt(responseObject, "PromptTokens")
                ?? ReadInt(responseObject, "InputTokens")
                ?? ReadInt(nestedResult, "PromptTokens")
                ?? ReadInt(nestedResult, "InputTokens");

            var completionTokens = ReadInt(usageObj, "CompletionTokens")
                ?? ReadInt(usageObj, "OutputTokens")
                ?? ReadInt(usageObj, "CompletionTokenCount")
                ?? ReadInt(usageObj, "OutputTokenCount")
                ?? ReadInt(responseObject, "CompletionTokens")
                ?? ReadInt(responseObject, "OutputTokens")
                ?? ReadInt(nestedResult, "CompletionTokens")
                ?? ReadInt(nestedResult, "OutputTokens");

            var totalTokens = ReadInt(usageObj, "TotalTokens")
                ?? ReadInt(usageObj, "TotalTokenCount")
                ?? ReadInt(responseObject, "TotalTokens")
                ?? ReadInt(nestedResult, "TotalTokens");

            var usageFromJson = ExtractUsageFromJson(responseObject);
            promptTokens ??= usageFromJson.PromptTokens;
            completionTokens ??= usageFromJson.CompletionTokens;
            totalTokens ??= usageFromJson.TotalTokens;

            if (!totalTokens.HasValue && (promptTokens.HasValue || completionTokens.HasValue))
                totalTokens = (promptTokens ?? 0) + (completionTokens ?? 0);

            return new LlmUsage(promptTokens, completionTokens, totalTokens);
        }

        private static LlmUsage ExtractUsageFromJson(object instance)
        {
            try
            {
                var json = JsonSerializer.Serialize(instance);
                using var doc = JsonDocument.Parse(json);

                var prompt = FindFirstInt(doc.RootElement,
                    "promptTokens", "promptTokenCount", "inputTokens", "inputTokenCount", "prompt_tokens", "input_tokens");
                var completion = FindFirstInt(doc.RootElement,
                    "completionTokens", "completionTokenCount", "outputTokens", "outputTokenCount", "completion_tokens", "output_tokens");
                var total = FindFirstInt(doc.RootElement,
                    "totalTokens", "totalTokenCount", "total_tokens");

                return new LlmUsage(prompt, completion, total);
            }
            catch
            {
                return new LlmUsage(null, null, null);
            }
        }

        private static int? FindFirstInt(JsonElement element, params string[] keys)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (keys.Any(k => property.Name.Equals(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var number))
                            return number;

                        if (property.Value.ValueKind == JsonValueKind.String
                            && int.TryParse(property.Value.GetString(), out var parsed))
                            return parsed;
                    }

                    var nested = FindFirstInt(property.Value, keys);
                    if (nested.HasValue)
                        return nested;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindFirstInt(item, keys);
                    if (nested.HasValue)
                        return nested;
                }
            }

            return null;
        }

        private static object? GetPropertyValue(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName);
            return property?.GetValue(instance);
        }

        private static int? ReadInt(object? instance, string propertyName)
        {
            if (instance is null)
                return null;

            var value = GetPropertyValue(instance, propertyName);
            if (value is null)
                return null;

            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                short shortValue => shortValue,
                byte byteValue => byteValue,
                JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out var number) => number,
                JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String && int.TryParse(jsonElement.GetString(), out var parsed) => parsed,
                _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }

        private static string BuildSystemPrompt(
            ApplicationSettingsService.AgentSystemPromptSettings prompts,
            AuthorizedTwitterUser auth,
            bool isSingleTweet,
            TwitterAPI.ThreadContext fullThread,
            bool forceNewsSearch,
            string preloadedNewsContext)
        {
            var sb = new StringBuilder();
            AppendPromptBlock(sb, prompts.IdentityPrompt);
            AppendPromptBlock(sb, prompts.ResponseRulesPrompt);
            sb.AppendLine();
            sb.AppendLine($"Status de autorizacao do usuario: {auth.Status}");
            sb.AppendLine($"Tipo de mencao: {(isSingleTweet ? "tweet simples (sem thread de debate)" : "mencao dentro de uma thread com debate")}");
            sb.AppendLine();

            if (auth.Status == AuthorizedTwitterUser.STATUS_INVITED)
            {
                AppendPromptBlock(sb, prompts.InvitedUserPrompt);
            }
            else
            {
                AppendPromptBlock(sb, prompts.AuthorizedCommandsPrompt);

                if (!isSingleTweet)
                {
                    AppendPromptBlock(sb, prompts.ThreadCommandsPrompt);
                }
                else
                {
                    AppendPromptBlock(sb, prompts.SingleTweetPrompt);
                }

                AppendPromptBlock(sb, prompts.FallbackPrompt);
            }

            if (forceNewsSearch)
            {
                sb.AppendLine();
                sb.AppendLine("PESQUISA EXTERNA OBRIGATORIA:");
                sb.AppendLine("- Esta mencao foi classificada como avaliacao factual/noticia.");
                sb.AppendLine("- A tool SearchNewsOnGoogle ja foi executada pelo worker antes da resposta final.");
                sb.AppendLine("- Use os resultados e links consultados como base da analise.");
                sb.AppendLine("- Para avaliacao factual, prefira resultado THREAD_FACT_TRUE, THREAD_FACT_FALSE ou THREAD_FACT_UNCERTAIN.");
                sb.AppendLine("- Nao afirme certeza se os resultados externos forem insuficientes ou contraditorios.");

                if (!string.IsNullOrWhiteSpace(preloadedNewsContext))
                {
                    sb.AppendLine();
                    sb.AppendLine("RESULTADO DA PESQUISA EXTERNA:");
                    sb.AppendLine(TruncateForPrompt(preloadedNewsContext, 5000));
                }
            }

            if (!isSingleTweet && fullThread != null && fullThread.Tweets.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(ThreadContextLabel);
                sb.AppendLine(fullThread.GetFullDialog());
            }

            sb.AppendLine();
            sb.AppendLine("PROTOCOLO DE RESPOSTA FINAL:");
            sb.AppendLine("- SearchNewsOnGoogle e ferramenta de apoio: ela pode ser chamada antes da ferramenta final e nao conta como acao final.");
            sb.AppendLine("- Quando a mencao pedir avaliacao de noticia, fato atual ou checagem factual, a consulta externa via SearchNewsOnGoogle e obrigatoria; se o bloco RESULTADO DA PESQUISA EXTERNA existir, ele ja cumpre essa etapa.");
            sb.AppendLine("- Chame uma ferramenta final para executar a acao ou consolidar os dados necessarios.");
            sb.AppendLine("- A ferramenta nao publica no Twitter; ela retorna dados para voce compor a resposta.");
            sb.AppendLine("- Depois da ferramenta, retorne uma resposta estruturada com:");
            sb.AppendLine("  - result: o codigo de resultado retornado pela ferramenta.");
            sb.AppendLine("  - text: o texto exato que deve ser publicado no Twitter.");
            sb.AppendLine("- O campo text deve ter no maximo 280 caracteres.");
            sb.AppendLine("- O campo text nao deve copiar a mencao original, o bloco de pesquisa, URLs RSS, logs, JSON ou conteudo bruto das fontes.");
            sb.AppendLine("- Se usar links na resposta publica, use no maximo uma URL de materia real; nunca use URL do Google News RSS.");
            sb.AppendLine("- Gere o campo text com a LLM usando as orientacoes deste system prompt e os dados retornados pela ferramenta.");
            sb.AppendLine("- Nao retorne texto fora da estrutura final.");

            return sb.ToString();
        }

        private static void AppendPromptBlock(StringBuilder sb, string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return;

            sb.AppendLine(prompt.Trim());
        }

        private static string ExtractFinalResponseText(
            TwitterAgentResponse response,
            string rawText,
            bool forceNewsSearch,
            string mentionText)
        {
            return AgentResponseRules.ExtractFinalResponseText(
                response?.Text,
                rawText,
                forceNewsSearch,
                mentionText);
        }

        private static bool LooksLikeContextEcho(string text, string mentionText)
        {
            return AgentResponseRules.LooksLikeContextEcho(text, mentionText);
        }

        private static string BuildAgentInput(string mentionText, bool forceNewsSearch, string preloadedNewsContext)
        {
            return mentionText ?? string.Empty;
        }

        private static bool ShouldForceNewsSearch(string mentionText, TwitterAPI.ThreadContext fullThread)
        {
            return NewsSearchRules.ShouldForceNewsSearch(
                mentionText,
                fullThread?.GetFullDialog() ?? string.Empty);
        }

        private static string BuildNewsSearchQuery(string mentionText, TwitterAPI.ThreadContext fullThread, string botUserId)
        {
            var threadTweets = fullThread?.Tweets
                .Select(tweet => new NewsSearchTweet(tweet.AuthorId, tweet.Text))
                .ToArray() ?? [];
            IEnumerable<string> fallbackThreadTexts = fullThread == null
                ? []
                : new[] { fullThread.GetStartA(), fullThread.GetStartB() };

            return NewsSearchRules.BuildNewsSearchQuery(
                mentionText,
                threadTweets,
                botUserId,
                fallbackThreadTexts);
        }

        private static string CleanNewsSearchCandidate(string value)
        {
            return NewsSearchRules.CleanNewsSearchCandidate(value);
        }

        private static string SelectBestNewsSearchCandidate(IReadOnlyCollection<string> candidates)
        {
            return NewsSearchRules.SelectBestNewsSearchCandidate(candidates);
        }

        private static bool LooksLikeOnlyFactCheckRequest(string value)
        {
            return NewsSearchRules.LooksLikeOnlyFactCheckRequest(value);
        }

        private static bool AreEquivalentSearchTexts(string left, string right)
        {
            return NewsSearchRules.AreEquivalentSearchTexts(left, right);
        }

        private static string NormalizeSearchText(string value)
        {
            return NewsSearchRules.NormalizeSearchText(value);
        }

        private static string TruncateForPrompt(string value, int maxLength)
        {
            return NewsSearchRules.TruncateForPrompt(value, maxLength);
        }

        private static TwitterAgentResponse TryParseAgentResponse(string text)
        {
            var parsed = AgentResponseRules.TryParseAgentResponse(text);
            if (parsed is null)
                return null;

            return new TwitterAgentResponse
            {
                Result = parsed.Result,
                Text = parsed.Text
            };
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

        private static string BuildMentionUrl(string tweetId)
        {
            if (string.IsNullOrWhiteSpace(tweetId))
                return string.Empty;

            return $"https://x.com/i/web/status/{tweetId.Trim()}";
        }

        private async Task<int> GetProcessorIdleDelaySecondsAsync(int currentIdleDelaySeconds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();
                var processorSettings = await settingsService.GetAgentProcessorSettingsAsync();
                var runtimeSettings = await settingsService.GetTwitterMentionsRuntimeSettingsAsync();

                _runtimeStore.ConfigureLimits(runtimeSettings.MaxQueueSize, runtimeSettings.MaxLogEntries);

                return processorSettings.IdleDelaySeconds;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nao foi possivel ler settings do processor. Mantendo delay atual.");
                return currentIdleDelaySeconds;
            }
        }

        private sealed class TwitterAgentResponse
        {
            [JsonPropertyName("result")]
            [Description("Codigo curto do resultado da acao, normalmente igual ao Result retornado pela ferramenta executada.")]
            public string Result { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            [Description("Texto final exato que sera publicado como resposta no Twitter/X.")]
            public string Text { get; set; } = string.Empty;
        }

        private sealed record LlmUsage(int? PromptTokens, int? CompletionTokens, int? TotalTokens);
    }
}
