using System.Collections.Concurrent;

namespace VeraciBot.App.Services
{
    public sealed record TwitterMentionQueueItem(
        string Id,
        string AuthorId,
        string Text,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset EnqueuedAtUtc
    );

    public sealed record TwitterWorkerLogEntry(
        DateTimeOffset TimestampUtc,
        string Source,
        string Level,
        string Message
    );

    public sealed record TwitterMentionsRuntimeStats(
        int TotalFetched,
        int TotalEnqueued,
        int TotalDequeued,
        int TotalProcessed,
        int TotalSkipped,
        int TotalErrors,
        int UnauthorizedMentions,
        int AgentReplies,
        int HelpRequests,
        int ScoreRequests,
        int ScoreboardRequests,
        int InviteRequests,
        int InviteErrors,
        int InvitesAccepted,
        int InvitesRefused,
        int UnknownCommands,
        int ThreadArgumentAnalyses,
        int ThreadFactChecks,
        int ThreadFactTrue,
        int ThreadFactFalse,
        int ThreadFactUncertain,
        int ThreadWhoIsRightAnalyses,
        int DebateAuthorAWins,
        int DebateAuthorBWins,
        int DebateDraws,
        IReadOnlyDictionary<string, int> ResultCounts
    );

    public sealed record TwitterMentionsRuntimeSnapshot(
        bool WorkerRunning,
        bool ProcessorRunning,
        DateTimeOffset? LastPollStartedAtUtc,
        DateTimeOffset? LastPollCompletedAtUtc,
        DateTimeOffset? LastProcessingStartedAtUtc,
        DateTimeOffset? LastProcessedAtUtc,
        DateTimeOffset CursorStartTimeUtc,
        int LastFetchedCount,
        int LastEnqueuedCount,
        string LastError,
        string LastProcessorError,
        string CurrentMentionId,
        string CurrentMentionAuthorId,
        TwitterMentionsRuntimeStats Stats,
        IReadOnlyList<TwitterMentionQueueItem> Queue,
        IReadOnlyList<TwitterWorkerLogEntry> TwitterLogs,
        IReadOnlyList<TwitterWorkerLogEntry> AgentLogs,
        IReadOnlyList<TwitterWorkerLogEntry> Logs
    );

    public sealed class TwitterMentionsRuntimeStore
    {
        private readonly ConcurrentQueue<TwitterMentionQueueItem> _queue = new();
        private readonly HashSet<string> _seenMentionIds = new(StringComparer.Ordinal);
        private readonly List<TwitterWorkerLogEntry> _logs = [];
        private readonly Dictionary<string, int> _resultCounts = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _sync = new();
        private int _maxQueueSize;
        private int _maxLogEntries;

        private bool _workerRunning;
        private bool _processorRunning;
        private DateTimeOffset? _lastPollStartedAtUtc;
        private DateTimeOffset? _lastPollCompletedAtUtc;
        private DateTimeOffset? _lastProcessingStartedAtUtc;
        private DateTimeOffset? _lastProcessedAtUtc;
        private DateTimeOffset _cursorStartTimeUtc = DateTimeOffset.UtcNow;
        private int _lastFetchedCount;
        private int _lastEnqueuedCount;
        private string _lastError = string.Empty;
        private string _lastProcessorError = string.Empty;
        private string _currentMentionId = string.Empty;
        private string _currentMentionAuthorId = string.Empty;
        private int _totalFetched;
        private int _totalEnqueued;
        private int _totalDequeued;
        private int _totalProcessed;
        private int _totalSkipped;
        private int _totalErrors;

        public event Action Updated;

        public TwitterMentionsRuntimeStore(
            int maxQueueSize = ApplicationSettingsService.DefaultTwitterWorkerMaxQueueSize,
            int maxLogEntries = ApplicationSettingsService.DefaultTwitterWorkerMaxLogEntries)
        {
            _maxQueueSize = Math.Max(100, maxQueueSize);
            _maxLogEntries = Math.Max(100, maxLogEntries);
        }

        public void ConfigureLimits(int maxQueueSize, int maxLogEntries)
        {
            lock (_sync)
            {
                _maxQueueSize = Math.Max(100, maxQueueSize);
                _maxLogEntries = Math.Max(100, maxLogEntries);
                TrimQueueUnsafe();
                TrimLogsUnsafe();
            }

            NotifyUpdated();
        }

        public void SetWorkerState(bool running)
        {
            lock (_sync)
            {
                _workerRunning = running;
            }

            NotifyUpdated();
        }

        public void SetProcessorState(bool running)
        {
            lock (_sync)
            {
                _processorRunning = running;

                if (!running)
                {
                    _currentMentionId = string.Empty;
                    _currentMentionAuthorId = string.Empty;
                    _lastProcessingStartedAtUtc = null;
                }
            }

            NotifyUpdated();
        }

        public void MarkPollStarted(DateTimeOffset cursorStartTimeUtc)
        {
            lock (_sync)
            {
                _lastPollStartedAtUtc = DateTimeOffset.UtcNow;
                _cursorStartTimeUtc = cursorStartTimeUtc;
            }

            NotifyUpdated();
        }

        public void MarkPollCompleted(int fetchedCount, int enqueuedCount, DateTimeOffset nextCursorStartTimeUtc)
        {
            lock (_sync)
            {
                _lastPollCompletedAtUtc = DateTimeOffset.UtcNow;
                _lastFetchedCount = fetchedCount;
                _lastEnqueuedCount = enqueuedCount;
                _cursorStartTimeUtc = nextCursorStartTimeUtc;
                _lastError = string.Empty;
                _totalFetched += fetchedCount;
            }

            NotifyUpdated();
        }

        public void MarkPollFailed(Exception exception)
        {
            lock (_sync)
            {
                _lastPollCompletedAtUtc = DateTimeOffset.UtcNow;
                _lastError = exception.Message;
            }

            AddTwitterLog("error", $"Falha no ciclo de leitura: {exception.Message}");
            NotifyUpdated();
        }

        public bool TryEnqueueMention(TwitterMentionQueueItem item)
        {
            var enqueued = false;

            lock (_sync)
            {
                if (!_seenMentionIds.Add(item.Id))
                    return false;

                _queue.Enqueue(item);
                enqueued = true;
                _totalEnqueued++;

                TrimQueueUnsafe();
            }

            if (enqueued)
                NotifyUpdated();

            return enqueued;
        }

        public void MarkProcessingStarted(TwitterMentionQueueItem item)
        {
            lock (_sync)
            {
                _lastProcessingStartedAtUtc = DateTimeOffset.UtcNow;
                _currentMentionId = item.Id;
                _currentMentionAuthorId = item.AuthorId;
                _lastProcessorError = string.Empty;
            }

            NotifyUpdated();
        }

        public void MarkProcessingCompleted(TwitterMentionQueueItem item, string result)
        {
            var normalizedResult = NormalizeResult(result);

            lock (_sync)
            {
                _totalProcessed++;
                _lastProcessedAtUtc = DateTimeOffset.UtcNow;
                _lastProcessorError = string.Empty;
                _currentMentionId = string.Empty;
                _currentMentionAuthorId = string.Empty;
                _lastProcessingStartedAtUtc = null;

                _resultCounts.TryGetValue(normalizedResult, out var currentCount);
                _resultCounts[normalizedResult] = currentCount + 1;
            }

            AddAgentLog("info", $"Menção {item.Id} concluída: {GetResultLabel(normalizedResult)}.");
        }

        public void MarkProcessingSkipped(TwitterMentionQueueItem item, string reason)
        {
            var normalizedReason = NormalizeResult(reason);

            lock (_sync)
            {
                _totalSkipped++;
                _lastProcessedAtUtc = DateTimeOffset.UtcNow;
                _currentMentionId = string.Empty;
                _currentMentionAuthorId = string.Empty;
                _lastProcessingStartedAtUtc = null;

                _resultCounts.TryGetValue(normalizedReason, out var currentCount);
                _resultCounts[normalizedReason] = currentCount + 1;
            }

            AddAgentLog("warning", $"Menção {item.Id} ignorada: {GetResultLabel(normalizedReason)}.");
        }

        public void MarkProcessingFailed(TwitterMentionQueueItem item, Exception exception)
        {
            lock (_sync)
            {
                _totalErrors++;
                _lastProcessedAtUtc = DateTimeOffset.UtcNow;
                _lastProcessorError = exception.Message;
                _currentMentionId = string.Empty;
                _currentMentionAuthorId = string.Empty;
                _lastProcessingStartedAtUtc = null;
            }

            AddAgentLog("error", $"Erro ao processar menção {item.Id}: {exception.Message}");
        }

        public TwitterMentionsRuntimeSnapshot GetSnapshot()
        {
            lock (_sync)
            {
                var orderedLogs = _logs
                    .OrderByDescending(x => x.TimestampUtc)
                    .ToList();
                var resultCounts = _resultCounts
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                return new TwitterMentionsRuntimeSnapshot(
                    WorkerRunning: _workerRunning,
                    ProcessorRunning: _processorRunning,
                    LastPollStartedAtUtc: _lastPollStartedAtUtc,
                    LastPollCompletedAtUtc: _lastPollCompletedAtUtc,
                    LastProcessingStartedAtUtc: _lastProcessingStartedAtUtc,
                    LastProcessedAtUtc: _lastProcessedAtUtc,
                    CursorStartTimeUtc: _cursorStartTimeUtc,
                    LastFetchedCount: _lastFetchedCount,
                    LastEnqueuedCount: _lastEnqueuedCount,
                    LastError: _lastError,
                    LastProcessorError: _lastProcessorError,
                    CurrentMentionId: _currentMentionId,
                    CurrentMentionAuthorId: _currentMentionAuthorId,
                    Stats: BuildStats(resultCounts),
                    Queue: _queue.ToArray()
                        .OrderByDescending(x => x.EnqueuedAtUtc)
                        .ToList(),
                    TwitterLogs: orderedLogs
                        .Where(x => x.Source == "Twitter")
                        .ToList(),
                    AgentLogs: orderedLogs
                        .Where(x => x.Source == "Agent")
                        .ToList(),
                    Logs: orderedLogs
                );
            }
        }

        public void AddLog(string level, string message)
        {
            AddTwitterLog(level, message);
        }

        public void AddTwitterLog(string level, string message)
        {
            AddLog("Twitter", level, message);
        }

        public void AddAgentLog(string level, string message)
        {
            AddLog("Agent", level, message);
        }

        private void AddLog(string source, string level, string message)
        {
            lock (_sync)
            {
                _logs.Add(new TwitterWorkerLogEntry(
                    TimestampUtc: DateTimeOffset.UtcNow,
                    Source: source,
                    Level: level,
                    Message: message
                ));

                TrimLogsUnsafe();
            }

            NotifyUpdated();
        }

        public bool TryDequeue(out TwitterMentionQueueItem item)
        {
            if (_queue.TryDequeue(out item))
            {
                lock (_sync)
                {
                    _totalDequeued++;
                }

                NotifyUpdated();
                return true;
            }

            return false;
        }

        private TwitterMentionsRuntimeStats BuildStats(IReadOnlyDictionary<string, int> resultCounts)
        {
            var unauthorized = GetCount(resultCounts, "NOT_AUTHORIZED");
            var skipped = GetCount(resultCounts, "PROCESSOR_DISABLED")
                + GetCount(resultCounts, "OPENAI_NOT_CONFIGURED")
                + GetCount(resultCounts, "ALREADY_PROCESSED");
            var threadFactTrue = GetCount(resultCounts, "THREAD_FACT_TRUE");
            var threadFactFalse = GetCount(resultCounts, "THREAD_FACT_FALSE");
            var threadFactUncertain = GetCount(resultCounts, "THREAD_FACT_UNCERTAIN")
                + GetCount(resultCounts, "THREAD_FALSE");
            var threadFactChecks = threadFactTrue + threadFactFalse + threadFactUncertain;

            return new TwitterMentionsRuntimeStats(
                TotalFetched: _totalFetched,
                TotalEnqueued: _totalEnqueued,
                TotalDequeued: _totalDequeued,
                TotalProcessed: _totalProcessed,
                TotalSkipped: Math.Max(_totalSkipped, skipped),
                TotalErrors: _totalErrors,
                UnauthorizedMentions: unauthorized,
                AgentReplies: Math.Max(0, _totalProcessed - unauthorized),
                HelpRequests: GetCount(resultCounts, "HELP"),
                ScoreRequests: GetCount(resultCounts, "SCORE"),
                ScoreboardRequests: GetCount(resultCounts, "SCOREBOARD"),
                InviteRequests: GetCount(resultCounts, "INVITE") + GetCount(resultCounts, "INVITE_NO_USER"),
                InviteErrors: GetCount(resultCounts, "INVITE_ERROR"),
                InvitesAccepted: GetCount(resultCounts, "ACCEPT"),
                InvitesRefused: GetCount(resultCounts, "REFUSE"),
                UnknownCommands: GetCount(resultCounts, "UNKNOWN") + GetCount(resultCounts, "NO_AGENT_RESPONSE"),
                ThreadArgumentAnalyses: GetCount(resultCounts, "THREAD_ARGUE_0")
                    + GetCount(resultCounts, "THREAD_ARGUE_1")
                    + GetCount(resultCounts, "THREAD_ARGUE_2"),
                ThreadFactChecks: threadFactChecks,
                ThreadFactTrue: threadFactTrue,
                ThreadFactFalse: threadFactFalse,
                ThreadFactUncertain: threadFactUncertain,
                ThreadWhoIsRightAnalyses: GetCount(resultCounts, "THREAD_WHOISRIGHT"),
                DebateAuthorAWins: GetCount(resultCounts, "THREAD_ARGUE_1"),
                DebateAuthorBWins: GetCount(resultCounts, "THREAD_ARGUE_2"),
                DebateDraws: GetCount(resultCounts, "THREAD_ARGUE_0"),
                ResultCounts: resultCounts
            );
        }

        public static string GetResultLabel(string result)
        {
            return NormalizeResult(result) switch
            {
                "HELP" => "ajuda",
                "SCORE" => "pontuação",
                "SCOREBOARD" => "placar",
                "INVITE" => "convite enviado",
                "INVITE_NO_USER" => "convite sem usuário",
                "INVITE_ERROR" => "erro de convite",
                "ACCEPT" => "convite aceito",
                "REFUSE" => "convite recusado",
                "UNKNOWN" => "comando desconhecido",
                "NOT_AUTHORIZED" => "usuário não autorizado",
                "THREAD_ARGUE_0" => "debate empatado",
                "THREAD_ARGUE_1" => "debate vencido pelo autor A",
                "THREAD_ARGUE_2" => "debate vencido pelo autor B",
                "THREAD_FACT_TRUE" => "checagem: verdadeiro",
                "THREAD_FACT_FALSE" => "checagem: falso",
                "THREAD_FACT_UNCERTAIN" => "checagem: inconclusivo",
                "THREAD_FALSE" => "checagem de fato",
                "THREAD_WHOISRIGHT" => "quem tem razão",
                "PROCESSOR_DISABLED" => "processor desativado",
                "OPENAI_NOT_CONFIGURED" => "OpenAI não configurada",
                "ALREADY_PROCESSED" => "já processada",
                "NO_AGENT_RESPONSE" => "sem resposta do agent",
                _ => NormalizeResult(result).ToLowerInvariant().Replace('_', ' ')
            };
        }

        private static int GetCount(IReadOnlyDictionary<string, int> counts, string key)
        {
            return counts.TryGetValue(key, out var count) ? count : 0;
        }

        private static string NormalizeResult(string result)
        {
            return string.IsNullOrWhiteSpace(result)
                ? "NO_AGENT_RESPONSE"
                : result.Trim().ToUpperInvariant();
        }

        private void TrimQueueUnsafe()
        {
            while (_queue.Count > _maxQueueSize && _queue.TryDequeue(out var dropped))
            {
                _seenMentionIds.Remove(dropped.Id);
            }
        }

        private void TrimLogsUnsafe()
        {
            if (_logs.Count > _maxLogEntries)
            {
                _logs.RemoveRange(0, _logs.Count - _maxLogEntries);
            }
        }

        private void NotifyUpdated()
        {
            Updated?.Invoke();
        }
    }
}
