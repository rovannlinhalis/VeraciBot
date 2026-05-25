namespace VeraciBot.Core.Entities
{
    public class AuthorizedTwitterUser
    {
        public const string STATUS_NOT_AUTHORIZED = "NOT_AUTHORIZED";
        public const string STATUS_INVITED = "INVITED";
        public const string STATUS_AUTHORIZED = "AUTHORIZED";

        public string AuthorId { get; set; } = string.Empty;
        public string Status { get; set; } = STATUS_NOT_AUTHORIZED;
        public string AuthorizedById { get; set; } = string.Empty;
        public DateTime AuthorizationDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeauthorizationDate { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int InviteCredits { get; set; }
        public int InvitesSent { get; set; }
        public int InvitesAccepted { get; set; }
        public int Score { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public class AuthorizedTwitterUserHistory
    {
        public long Id { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PreviousStatus { get; set; } = string.Empty;
        public string Status { get; set; } = AuthorizedTwitterUser.STATUS_NOT_AUTHORIZED;
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
        public long? ApplicationUserId { get; set; }
        public long? ChangedByApplicationUserId { get; set; }
        public string ChangedByAuthorId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
