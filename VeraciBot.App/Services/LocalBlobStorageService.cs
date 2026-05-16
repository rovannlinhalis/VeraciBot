using System.Security.Cryptography;
using VeraciBot.App.Interfaces;
using VeraciBot.App.Model;

namespace VeraciBot.App.Services
{
    public class LocalBlobStorageService : IBlobStorageService
    {
        private const int BufferSize = 81920;

        private readonly string _rootPath;
        private readonly string _rootPathWithSeparator;
        private readonly string _publicPath;

        public LocalBlobStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
            var configuredRootPath = configuration["BlobStorage:LocalPath"];

            _rootPath = Path.GetFullPath(
                string.IsNullOrWhiteSpace(configuredRootPath)
                    ? Path.Combine(webRootPath, "uploads")
                    : configuredRootPath);

            _rootPathWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            _publicPath = NormalizePublicPath(configuration["BlobStorage:PublicPath"] ?? "uploads");

            Directory.CreateDirectory(_rootPath);
        }

        public Task<bool> DeleteFileAsync(string objectName, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var filePath = ResolvePath(objectName);
            if (!File.Exists(filePath))
                return Task.FromResult(false);

            File.Delete(filePath);
            return Task.FromResult(true);
        }

        public Task<bool> FileExists(string filePath, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(false);

            return Task.FromResult(File.Exists(ResolvePath(filePath)));
        }

        public Task<long> GetBlobSizeAsync(string blobPath, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var filePath = ResolvePath(blobPath);
            return Task.FromResult(File.Exists(filePath) ? new FileInfo(filePath).Length : 0);
        }

        public string GetContainerSASToken(int expirityMinutes)
        {
            return string.Empty;
        }

        public Task<string> GetPresignedUrl(string objectName, string currentSAS = null)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return Task.FromResult(objectName);

            if (Uri.TryCreate(objectName, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return Task.FromResult(objectName);
            }

            var normalizedObjectName = NormalizeObjectName(objectName);
            if (File.Exists(ResolvePath(normalizedObjectName)))
                return Task.FromResult(BuildPublicUrl(normalizedObjectName));

            return Task.FromResult(BuildAppRelativeUrl(normalizedObjectName));
        }

        public async Task<MemoryStream> OpenFileMemory(string fileName, CancellationToken ct = default)
        {
            var filePath = ResolvePath(fileName);
            var memoryStream = new MemoryStream();

            await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
            {
                await fileStream.CopyToAsync(memoryStream, ct);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task<UploadFileResult> UploadFileAsync(
            string objectName,
            Stream data,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            var normalizedObjectName = NormalizeObjectName(objectName);
            var filePath = ResolvePath(normalizedObjectName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                await data.CopyToAsync(fileStream, cancellationToken);
            }

            return new UploadFileResult
            {
                FilePath = normalizedObjectName,
                FileUrl = BuildPublicUrl(normalizedObjectName),
                Hash = await GetSha1Async(normalizedObjectName, cancellationToken)
            };
        }

        public async Task<UploadFileResult> UploadWithProgressAsync(
            Stream stream,
            string fileName,
            string contentType,
            IProgress<long> progress,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var normalizedObjectName = NormalizeObjectName(fileName);
            var filePath = ResolvePath(normalizedObjectName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            var totalBytes = 0L;
            var buffer = new byte[BufferSize];

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    totalBytes += bytesRead;
                    progress?.Report(totalBytes);
                }
            }

            return new UploadFileResult
            {
                FilePath = normalizedObjectName,
                FileUrl = BuildPublicUrl(normalizedObjectName),
                Hash = await GetSha1Async(normalizedObjectName, cancellationToken)
            };
        }

        public async Task<bool> DownloadFileAsync(string file, string destiny, CancellationToken cancellationToken = default)
        {
            var sourcePath = ResolvePath(file);
            if (!File.Exists(sourcePath))
                return false;

            var destinationDirectory = Path.GetDirectoryName(destiny);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            await using var destinationStream = new FileStream(destiny, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken);

            return true;
        }

        public async Task<string> GetSha1Async(string objectName, CancellationToken cancellationToken = default)
        {
            var filePath = ResolvePath(objectName);
            if (!File.Exists(filePath))
                return null;

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            using var sha1 = SHA1.Create();

            var hash = await sha1.ComputeHashAsync(fileStream, cancellationToken);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string ResolvePath(string objectName)
        {
            var normalizedObjectName = NormalizeObjectName(objectName);
            var filePath = Path.GetFullPath(Path.Combine(_rootPath, normalizedObjectName.Replace('/', Path.DirectorySeparatorChar)));

            if (!filePath.StartsWith(_rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Caminho de arquivo fora da pasta de armazenamento local.");

            return filePath;
        }

        private string NormalizeObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("O nome do arquivo não pode ser vazio.", nameof(objectName));

            var normalized = objectName.Trim().Replace('\\', '/');
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
                normalized = uri.AbsolutePath;

            normalized = normalized.TrimStart('/');

            if (!string.IsNullOrWhiteSpace(_publicPath) &&
                normalized.StartsWith(_publicPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[(_publicPath.Length + 1)..];
            }

            var segments = normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length == 0 || segments.Any(x => x == "." || x == ".."))
                throw new ArgumentException("Nome de arquivo inválido.", nameof(objectName));

            return string.Join('/', segments);
        }

        private string BuildPublicUrl(string objectName)
        {
            var normalizedObjectName = NormalizeObjectName(objectName);
            var escapedObjectName = EscapeObjectName(normalizedObjectName);

            return string.IsNullOrWhiteSpace(_publicPath)
                ? "/" + escapedObjectName
                : "/" + _publicPath + "/" + escapedObjectName;
        }

        private static string BuildAppRelativeUrl(string objectName)
        {
            return "/" + EscapeObjectName(objectName);
        }

        private static string EscapeObjectName(string objectName)
        {
            return string.Join(
                '/',
                objectName.Split('/').Select(Uri.EscapeDataString));
        }

        private static string NormalizePublicPath(string publicPath)
        {
            return string.IsNullOrWhiteSpace(publicPath)
                ? string.Empty
                : publicPath.Trim().Replace('\\', '/').Trim('/');
        }
    }
}
