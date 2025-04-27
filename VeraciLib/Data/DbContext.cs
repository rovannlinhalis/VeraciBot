
namespace VeraciBot.Data;

// Contexto do banco de dados
using Microsoft.EntityFrameworkCore;
using VeraciBot;

public class VeraciDbContext : DbContext
{

    public DbSet<Tweet> Tweets{ get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        
        // String de conexão para seu SQL Server local
        optionsBuilder.UseSqlServer(AppKeys.keys.dbConnection);

    }

}
