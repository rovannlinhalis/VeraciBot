using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using VeraciBot.Infrastructure.Storage;

namespace VeraciBot.Tests.Infrastructure
{
    public class LocalBlobStorageServiceTests : IDisposable
    {
        private readonly string rootPath = Path.Combine(Path.GetTempPath(), "VeraciBot.Tests", Guid.NewGuid().ToString("N"));

        [Fact]
        public async Task UploadFileAsync_ShouldStoreFileAndReturnPublicUrlAndHash()
        {
            var service = CreateService();
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("hello"));

            var result = await service.UploadFileAsync("nested/file.txt", stream, "text/plain");

            result.FilePath.Should().Be("nested/file.txt");
            result.FileUrl.Should().Be("/assets/nested/file.txt");
            result.Hash.Should().Be("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
            (await service.FileExists("nested/file.txt")).Should().BeTrue();
        }

        [Fact]
        public async Task UploadFileAsync_ShouldRejectPathTraversal()
        {
            var service = CreateService();
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("evil"));

            var act = () => service.UploadFileAsync("../evil.txt", stream, "text/plain");

            await act.Should().ThrowAsync<ArgumentException>();
        }

        public void Dispose()
        {
            if (Directory.Exists(rootPath))
                Directory.Delete(rootPath, recursive: true);
        }

        private LocalBlobStorageService CreateService()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["BlobStorage:LocalPath"] = rootPath,
                    ["BlobStorage:PublicPath"] = "assets"
                })
                .Build();

            return new LocalBlobStorageService(config);
        }
    }
}
