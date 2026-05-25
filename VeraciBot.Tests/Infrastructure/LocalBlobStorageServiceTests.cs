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

        [Fact]
        public async Task GetPresignedUrl_ShouldReturnAbsoluteUrlUnchanged()
        {
            var service = CreateService();

            var url = await service.GetPresignedUrl("https://cdn.example.com/assets/file.png");

            url.Should().Be("https://cdn.example.com/assets/file.png");
        }

        [Fact]
        public async Task GetPresignedUrl_ShouldNormalizeConfiguredPublicPathAndEscapeSegments()
        {
            var service = CreateService();
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            await service.UploadFileAsync("nested/file name.txt", stream, "text/plain");

            var url = await service.GetPresignedUrl("/assets/nested/file name.txt");

            url.Should().Be("/assets/nested/file%20name.txt");
        }

        [Fact]
        public async Task UploadWithProgressAsync_ShouldReportBytesAndSupportReadDownloadAndDelete()
        {
            var service = CreateService();
            var progressValues = new List<long>();
            var progress = new RecordingProgress<long>(progressValues.Add);
            await using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("abcdef"));

            var result = await service.UploadWithProgressAsync(
                uploadStream,
                "folder/data.txt",
                "text/plain",
                progress);

            result.FilePath.Should().Be("folder/data.txt");
            progressValues.Should().Contain(6);
            (await service.GetBlobSizeAsync("folder/data.txt")).Should().Be(6);

            await using var stored = await service.OpenFileMemory("folder/data.txt");
            Encoding.UTF8.GetString(stored.ToArray()).Should().Be("abcdef");

            var downloadPath = Path.Combine(rootPath, "downloads", "data.txt");
            (await service.DownloadFileAsync("folder/data.txt", downloadPath)).Should().BeTrue();
            File.ReadAllText(downloadPath).Should().Be("abcdef");

            (await service.DeleteFileAsync("folder/data.txt")).Should().BeTrue();
            (await service.FileExists(result.FilePath)).Should().BeFalse();
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

        private sealed class RecordingProgress<T>(Action<T> onReport) : IProgress<T>
        {
            public void Report(T value)
            {
                onReport(value);
            }
        }
    }
}
