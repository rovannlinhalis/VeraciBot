using VeraciBot.Core.Models;

namespace VeraciBot.Core.Interfaces
{
    public interface IBlobStorageService
    {
        Task<bool> DeleteFileAsync(string objectName, CancellationToken ct = default);
        Task<bool> FileExists(string filePath, CancellationToken ct = default);
        Task<long> GetBlobSizeAsync(string blobPath, CancellationToken ct = default);
        string GetContainerSASToken(int expirityMinutes);
        Task<string> GetPresignedUrl(string objectName, string currentSAS = null);
        Task<MemoryStream> OpenFileMemory(string fileName, CancellationToken ct = default);
        Task<UploadFileResult> UploadFileAsync(string objectName, Stream data, string contentType, CancellationToken cancellationToken = default);
        Task<UploadFileResult> UploadWithProgressAsync(Stream stream, string fileName, string contentType, IProgress<long> progress, CancellationToken cancellationToken = default);
        Task<bool> DownloadFileAsync(string file, string destiny, CancellationToken cancellationToken = default);
        Task<string> GetSha1Async(string objectName, CancellationToken cancellationToken = default);
    }
}
