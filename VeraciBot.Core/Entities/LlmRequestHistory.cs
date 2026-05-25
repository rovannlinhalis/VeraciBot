namespace VeraciBot.Core.Entities
{
    public class LlmRequestHistory
    {
        public long Id { get; set; }
        public string ProcessedMentionTweetId { get; set; } = string.Empty;
        public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Model { get; set; } = string.Empty;
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public string LlmResult { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string MetadataJson { get; set; } = string.Empty;
        public string ConsultedNewsLinksJson { get; set; } = string.Empty;
        public string ProcessStepsJson { get; set; } = string.Empty;

        public ProcessedMention ProcessedMention { get; set; }
    }
}
