
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using Veracibot.API.Bot;
using Veracibot.API.Data;
using Veracibot.API.Models;

namespace Veracibot.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigurationManager configuration = builder.Configuration;


            builder.Services.Configure<TweeterOptions>(configuration.GetSection(TweeterOptions.SectionName));
            builder.Services.Configure<OpenAIOptions>(configuration.GetSection(TweeterOptions.SectionName));
            builder.Services.Configure<VeracibotOptions>(configuration.GetSection(TweeterOptions.SectionName));


            // Add services to the container.
            builder.Services.AddDbContext<VeracibotDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
            //eu usaria postgresql, SQLite é só pratico pra testes

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            SQLitePCL.Batteries.Init();

            builder.Services.AddHostedService<VeracibotBalanceWorker>();
            builder.Services.AddHostedService<VeracibotOpenAIWorker>();
            builder.Services.AddHostedService<VeracibotTweetWorker>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VeracibotDbContext>();

                if (!await db.Database.EnsureCreatedAsync())
                {
                    await db.Database.MigrateAsync();
                }
                await db.SaveChangesAsync();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
