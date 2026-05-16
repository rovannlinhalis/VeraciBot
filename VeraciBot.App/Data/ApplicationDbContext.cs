using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeraciBot.App.Entities;
using VeraciBot.App.Shared;

namespace VeraciBot.App.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser,ApplicationRole,long>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.Enabled).HasDefaultValue(true);
                entity.Property(x => x.TwitterUsername).HasMaxLength(64);
                entity.HasMany(x => x.UserRoles).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationUserRoles>(entity =>
            {
                entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ApplicationSettings>(entity => {
                entity.ToTable("application_settings");
                entity
                    .Property(x => x.Value)
                    .HasColumnType("TEXT")
                    .HasConversion(x => x.Encrypt(), x => x.Decrypt());
                entity.HasKey(x => x.Id);
            });

            builder.Entity<AuthorizedTwitterUser>(entity =>
            {
                entity.ToTable("authorized_twitter_users");
                entity.HasKey(x => x.AuthorId);
                entity.HasIndex(x => x.Status);
                entity.Property(x => x.Status).HasMaxLength(32);
                entity.Property(x => x.Username).HasMaxLength(64);
                entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            builder.Entity<TwitterInvite>(entity =>
            {
                entity.ToTable("twitter_invites");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.InviterAuthorId);
                entity.HasIndex(x => x.InviteeAuthorId);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.CreatedAtUtc);
                entity.Property(x => x.InviterAuthorId).HasMaxLength(64);
                entity.Property(x => x.InviterUsername).HasMaxLength(64);
                entity.Property(x => x.InviteeAuthorId).HasMaxLength(64);
                entity.Property(x => x.InviteeUsername).HasMaxLength(64);
                entity.Property(x => x.Status).HasMaxLength(32);
                entity.Property(x => x.SourceTweetId).HasMaxLength(64);
            });

            builder.Entity<TwitterInviteCreditTransaction>(entity =>
            {
                entity.ToTable("twitter_invite_credit_transactions");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.AuthorId);
                entity.HasIndex(x => x.CreatedAtUtc);
                entity.Property(x => x.AuthorId).HasMaxLength(64);
                entity.Property(x => x.Username).HasMaxLength(64);
                entity.Property(x => x.ChangedByAuthorId).HasMaxLength(64);
                entity.Property(x => x.Reason).HasMaxLength(64);
            });

            builder.Entity<AuthorizedTwitterUserHistory>(entity =>
            {
                entity.ToTable("authorized_twitter_user_history");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.AuthorId);
                entity.HasIndex(x => x.ChangedAtUtc);
                entity.Property(x => x.PreviousStatus).HasMaxLength(32);
                entity.Property(x => x.Status).HasMaxLength(32);
                entity.Property(x => x.Username).HasMaxLength(64);
            });

            builder.Entity<ProcessedMention>(entity =>
            {
                entity.ToTable("processed_mentions");
                entity.HasKey(x => x.TweetId);
                entity.HasIndex(x => x.AuthorId);
                entity.HasIndex(x => x.ProcessedAtUtc);
                entity.HasIndex(x => x.Result);
                entity.Property(x => x.MentionUrl).HasMaxLength(256);
                entity.Property(x => x.AuthorId).HasMaxLength(64);
                entity.Property(x => x.Result).HasMaxLength(64);
            });

            builder.Entity<LlmRequestHistory>(entity =>
            {
                entity.ToTable("llm_request_history");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.ProcessedMentionTweetId);
                entity.HasIndex(x => x.RequestedAtUtc);
                entity.HasIndex(x => x.Model);
                entity.Property(x => x.ProcessedMentionTweetId).HasMaxLength(450);
                entity.Property(x => x.Model).HasMaxLength(128);
                entity.Property(x => x.LlmResult).HasMaxLength(64);
                entity.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.ConsultedNewsLinksJson).HasColumnType("nvarchar(max)");
                entity.Property(x => x.ProcessStepsJson).HasColumnType("nvarchar(max)");

                entity.HasOne(x => x.ProcessedMention)
                    .WithMany(x => x.LlmRequests)
                    .HasForeignKey(x => x.ProcessedMentionTweetId)
                    .HasPrincipalKey(x => x.TweetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public virtual DbSet<ApplicationSettings> ApplicationSettings { get; set; }
        public virtual DbSet<AuthorizedTwitterUser> AuthorizedTwitterUsers { get; set; }
        public virtual DbSet<AuthorizedTwitterUserHistory> AuthorizedTwitterUserHistory { get; set; }
        public virtual DbSet<TwitterInvite> TwitterInvites { get; set; }
        public virtual DbSet<TwitterInviteCreditTransaction> TwitterInviteCreditTransactions { get; set; }
        public virtual DbSet<ProcessedMention> ProcessedMentions { get; set; }
        public virtual DbSet<LlmRequestHistory> LlmRequestHistory { get; set; }
    }
}
