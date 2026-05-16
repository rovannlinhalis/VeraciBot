using Microsoft.EntityFrameworkCore;
using VeraciBot.App.Data;
using VeraciBot.App.External;

namespace VeraciBot.App.Services
{
    public sealed class TwitterMentionsBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TwitterMentionsRuntimeStore _runtimeStore;
        private readonly ILogger<TwitterMentionsBackgroundWorker> _logger;

        public TwitterMentionsBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            TwitterMentionsRuntimeStore runtimeStore,
            ILogger<TwitterMentionsBackgroundWorker> logger
        )
        {
            _scopeFactory = scopeFactory;
            _runtimeStore = runtimeStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cursorStartTimeUtc = DateTimeOffset.UtcNow;
            var lastSeenMentionId = string.Empty;
            var cursorInitialized = false;
            var processedCursorInitialized = false;

            _runtimeStore.SetWorkerState(true);
            _runtimeStore.AddLog("info", "Twitter worker iniciado.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var pollIntervalSeconds = ApplicationSettingsService.DefaultTwitterWorkerPollIntervalSeconds;

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var twitterApi = scope.ServiceProvider.GetRequiredService<TwitterAPI>();
                        var settingsService =
                            scope.ServiceProvider.GetRequiredService<ApplicationSettingsService>();

                        var workerSettings =
                            await settingsService.GetTwitterMentionsWorkerSettingsAsync();
                        var runtimeSettings =
                            await settingsService.GetTwitterMentionsRuntimeSettingsAsync();

                        pollIntervalSeconds = workerSettings.PollIntervalSeconds;
                        _runtimeStore.ConfigureLimits(
                            runtimeSettings.MaxQueueSize,
                            runtimeSettings.MaxLogEntries
                        );

                        if (!cursorInitialized)
                        {
                            cursorStartTimeUtc =
                                workerSettings.StartTimeUtc
                                ?? DateTimeOffset.UtcNow.AddMinutes(
                                    -workerSettings.InitialLookbackMinutes
                                );
                            cursorInitialized = true;
                        }

                        if (!processedCursorInitialized)
                        {
                            lastSeenMentionId = await GetLatestProcessedMentionIdAsync(db, stoppingToken);
                            processedCursorInitialized = true;

                            if (!string.IsNullOrWhiteSpace(lastSeenMentionId))
                            {
                                _runtimeStore.AddLog(
                                    "info",
                                    $"Cursor inicial carregado a partir da ultima mencao processada: {lastSeenMentionId}."
                                );
                            }
                        }

                        if (!workerSettings.Enabled)
                        {
                            _runtimeStore.AddLog(
                                "info",
                                "Twitter worker desativado por ApplicationSettings."
                            );
                        }
                        else if (string.IsNullOrWhiteSpace(workerSettings.UserId))
                        {
                            _runtimeStore.AddLog(
                                "warning",
                                "TWITTER_USER_ID nao configurado. Worker aguardando configuracao."
                            );
                        }
                        else
                        {
                            _runtimeStore.MarkPollStarted(cursorStartTimeUtc);

                            var hasSinceId = !string.IsNullOrWhiteSpace(lastSeenMentionId);
                            var usedRecoveryLookback = false;
                            var mentions = await twitterApi.GetMentionsSinceAsync(
                                workerSettings.UserId,
                                cursorStartTimeUtc,
                                workerSettings.MaxResults,
                                lastSeenMentionId
                            );

                            if (mentions.Count == 0 && hasSinceId)
                            {
                                var recoveryStartTimeUtc = DateTimeOffset.UtcNow.AddMinutes(
                                    -workerSettings.InitialLookbackMinutes
                                );

                                mentions = await twitterApi.GetMentionsSinceAsync(
                                    workerSettings.UserId,
                                    recoveryStartTimeUtc,
                                    workerSettings.MaxResults,
                                    string.Empty
                                );
                                usedRecoveryLookback = true;

                                _runtimeStore.AddLog(
                                    "info",
                                    $"Consulta com since_id nao retornou mencoes. Recuperacao por start_time desde {recoveryStartTimeUtc:yyyy-MM-dd HH:mm:ss} UTC retornou {mentions.Count}."
                                );
                            }

                            var processedMentionIds = await GetProcessedMentionIdsAsync(db, mentions, stoppingToken);
                            var enqueuedCount = 0;
                            var alreadyProcessedCount = 0;
                            var alreadyQueuedCount = 0;
                            var newestMentionDate = cursorStartTimeUtc;
                            var newestMentionId = lastSeenMentionId;

                            foreach (var mention in mentions.OrderBy(x => x.CreatedAtUtc))
                            {
                                if (mention.CreatedAtUtc > newestMentionDate)
                                    newestMentionDate = mention.CreatedAtUtc;

                                if (IsTweetIdGreater(mention.Id, newestMentionId))
                                    newestMentionId = mention.Id;

                                if (processedMentionIds.Contains(mention.Id))
                                {
                                    alreadyProcessedCount++;
                                    continue;
                                }

                                var queued = _runtimeStore.TryEnqueueMention(
                                    new TwitterMentionQueueItem(
                                        Id: mention.Id,
                                        AuthorId: mention.AuthorId,
                                        Text: mention.Text,
                                        CreatedAtUtc: mention.CreatedAtUtc,
                                        EnqueuedAtUtc: DateTimeOffset.UtcNow
                                    )
                                );

                                if (queued)
                                {
                                    enqueuedCount++;
                                }
                                else
                                {
                                    alreadyQueuedCount++;
                                }
                            }

                            if (mentions.Count > 0)
                            {
                                lastSeenMentionId = newestMentionId;
                                cursorStartTimeUtc = newestMentionDate.AddSeconds(
                                    workerSettings.CursorAdvanceSeconds
                                );
                            }
                            else
                            {
                                if (hasSinceId)
                                {
                                    cursorStartTimeUtc = DateTimeOffset.UtcNow.AddSeconds(
                                        -workerSettings.EmptyLookbackSeconds
                                    );
                                }
                            }

                            _runtimeStore.MarkPollCompleted(
                                mentions.Count,
                                enqueuedCount,
                                cursorStartTimeUtc
                            );
                            _runtimeStore.AddLog(
                                "info",
                                $"Ciclo concluido{(usedRecoveryLookback ? " em recuperacao" : string.Empty)}. Lidas: {mentions.Count}, enfileiradas: {enqueuedCount}, ja processadas: {alreadyProcessedCount}, ja na fila: {alreadyQueuedCount}."
                            );
                        }
                    }
                    catch (TwitterApiException ex) when (ex.IsCreditsDepleted)
                    {
                        _logger.LogWarning(ex, "Creditos da API do X/Twitter esgotados.");
                        _runtimeStore.MarkPollFailed(ex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao ler mencoes do Twitter.");
                        _runtimeStore.MarkPollFailed(ex);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Encerramento esperado.
            }
            finally
            {
                _runtimeStore.SetWorkerState(false);
                _runtimeStore.AddLog("info", "Twitter worker finalizado.");
            }
        }

        private static bool IsTweetIdGreater(string candidateId, string currentId)
        {
            if (string.IsNullOrWhiteSpace(candidateId))
                return false;

            if (string.IsNullOrWhiteSpace(currentId))
                return true;

            if (ulong.TryParse(candidateId, out var candidate)
                && ulong.TryParse(currentId, out var current))
            {
                return candidate > current;
            }

            return string.CompareOrdinal(candidateId, currentId) > 0;
        }

        private static async Task<string> GetLatestProcessedMentionIdAsync(
            ApplicationDbContext db,
            CancellationToken cancellationToken)
        {
            var recentIds = await db.ProcessedMentions
                .AsNoTracking()
                .OrderByDescending(x => x.ProcessedAtUtc)
                .Take(500)
                .Select(x => x.TweetId)
                .ToListAsync(cancellationToken);

            var latestId = string.Empty;
            foreach (var id in recentIds)
            {
                if (IsTweetIdGreater(id, latestId))
                    latestId = id;
            }

            return latestId;
        }

        private static async Task<HashSet<string>> GetProcessedMentionIdsAsync(
            ApplicationDbContext db,
            IReadOnlyList<TwitterAPI.MentionContext> mentions,
            CancellationToken cancellationToken)
        {
            if (mentions.Count == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            var mentionIds = mentions
                .Select(x => x.Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (mentionIds.Length == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            var processedIds = await db.ProcessedMentions
                .AsNoTracking()
                .Where(x => mentionIds.Contains(x.TweetId))
                .Select(x => x.TweetId)
                .ToListAsync(cancellationToken);

            return processedIds.ToHashSet(StringComparer.Ordinal);
        }
    }
}
