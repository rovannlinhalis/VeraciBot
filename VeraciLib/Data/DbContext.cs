using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeraciBot;

namespace VeraciBot.Data
{

    public class VeraciDbContext(DbContextOptions<VeraciDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {

        public DbSet<Tweet> Tweets { get; set; }
        public DbSet<TweetAuthor> TweetAuthors { get; set; }
        public DbSet<Config> Configs { get; set; }

    }

}