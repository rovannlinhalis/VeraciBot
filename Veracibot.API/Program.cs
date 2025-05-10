
using Microsoft.EntityFrameworkCore;
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
                //options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                
                ); 

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Veracibot API",
                    Version = "v1",
                    Description = "Veracibot API"
                });
            });

            //builder.Services.AddHostedService<VeracibotBalanceWorker>();
            //builder.Services.AddHostedService<VeracibotOpenAIWorker>();
            //builder.Services.AddHostedService<VeracibotTweetWorker>();

            builder.Services.AddCors(options => {
                options.AddDefaultPolicy(policy => {
                    policy
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowAnyOrigin();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VeracibotDbContext>();
                await db.Database.MigrateAsync();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseDefaultFiles();  // Procura por index.html automaticamente
            app.UseStaticFiles();   // Habilita o uso de arquivos em wwwroot

            app.Run();
        }
    }
}
