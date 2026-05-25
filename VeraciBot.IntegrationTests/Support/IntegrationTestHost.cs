using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VeraciBot.App.Data;
using VeraciBot.Application.Services;
using VeraciBot.Core.Interfaces;
using VeraciBot.Core.Shared;
using VeraciBot.Infrastructure.Storage;

namespace VeraciBot.IntegrationTests.Support
{
    internal sealed class IntegrationTestHost : IDisposable, IAsyncDisposable
    {
        private const string EncryptionKey = "12345678901234567890123456789012";

        private IntegrationTestHost(ServiceProvider services, string rootPath)
        {
            Services = services;
            RootPath = rootPath;
        }

        public ServiceProvider Services { get; }
        public string RootPath { get; }

        public static IntegrationTestHost Create(Dictionary<string, string> configurationValues = null)
        {
            EncryptTool.Configure(EncryptionKey);

            var rootPath = Path.Combine(
                Path.GetTempPath(),
                "VeraciBot.IntegrationTests",
                Guid.NewGuid().ToString("N"));
            var databaseName = Guid.NewGuid().ToString("N");

            var values = new Dictionary<string, string>
            {
                ["BlobStorage:LocalPath"] = rootPath,
                ["BlobStorage:PublicPath"] = "assets"
            };

            foreach (var item in configurationValues ?? [])
                values[item.Key] = item.Value;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHttpClientFactory, ThrowingHttpClientFactory>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddScoped<IBlobStorageService, LocalBlobStorageService>();
            services.AddScoped<ApplicationSettingsService>();
            services.AddScoped<TwitterBotAuthenticationService>();

            return new IntegrationTestHost(
                services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                }),
                rootPath);
        }

        public async Task EnsureDatabaseCreatedAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public void Dispose()
        {
            Services.Dispose();

            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private sealed class ThrowingHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                return new HttpClient(new ThrowingHttpMessageHandler());
            }
        }

        private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                throw new InvalidOperationException(
                    $"Chamada HTTP externa nao esperada no teste de integracao: {request.Method} {request.RequestUri}");
            }
        }
    }
}
