namespace VeraciBot.Core.Entities
{
    public class TwitterInvite
    {
        public const string STATUS_PENDING = "PENDING";
        public const string STATUS_ACCEPTED = "ACCEPTED";
        public const string STATUS_REFUSED = "REFUSED";

        public long Id { get; set; }
        public string InviterAuthorId { get; set; } = string.Empty;
        public string InviterUsername { get; set; } = string.Empty;
        public string InviteeAuthorId { get; set; } = string.Empty;
        public string InviteeUsername { get; set; } = string.Empty;
        public string InviteeName { get; set; } = string.Empty;
        public string Status { get; set; } = STATUS_PENDING;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAtUtc { get; set; }
        public DateTime? RefusedAtUtc { get; set; }
        public string SourceTweetId { get; set; } = string.Empty;
        public long? CreatedByApplicationUserId { get; set; }
    }

    public class TwitterInviteCreditTransaction
    {
        public long Id { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Delta { get; set; }
        public int BalanceAfter { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public long? ChangedByApplicationUserId { get; set; }
        public string ChangedByAuthorId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
