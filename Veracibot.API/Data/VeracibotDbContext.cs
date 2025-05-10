using Microsoft.EntityFrameworkCore;
using Veracibot.API.Models;

namespace Veracibot.API.Data
{
    public class VeracibotDbContext : DbContext
    {
        public VeracibotDbContext(DbContextOptions<VeracibotDbContext> options) : base(options)
        {


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSnakeCaseNamingConvention();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        property.SetMaxLength(255);
                    }
                }
            }


            builder.Entity<Tweets>(entity =>
            {
                entity.ToTable("tweets");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.ThreadId);
                entity.HasIndex(e => e.OriginalAuthorId);
                entity.Property(x=>x.Text).HasColumnType("TEXT");
                entity.Property(x => x.OriginalText).HasColumnType("TEXT");
            });

            builder.Entity<TweetAuthor>(entity =>
            {
                entity.ToTable("tweet_authors");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserName);
            });

            builder.Entity<AuthorBalance>(entity =>
            {
                entity.ToTable("author_balance");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e=> e.AuthorId);
                entity.HasIndex(x => x.Type);
            });
        }
        public DbSet<Tweets> Tweets { get; set; }
        public DbSet<TweetAuthor> TweetAuthors { get; set; }
        public DbSet<AuthorBalance> AuthorBalances { get; set; }
    }
}
