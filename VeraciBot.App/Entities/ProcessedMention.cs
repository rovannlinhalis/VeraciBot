namespace VeraciBot.App.Entities
{
    public class ProcessedMention
    {
        public string TweetId { get; set; } = string.Empty;
        public string MentionUrl { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTimeOffset ProcessedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public string Result { get; set; } = string.Empty;

        public ICollection<LlmRequestHistory> LlmRequests { get; set; } = new List<LlmRequestHistory>();
    }
}
