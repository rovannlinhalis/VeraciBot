using Microsoft.Extensions.Configuration;

namespace VeraciBot.Infrastructure.Storage
{
    public static class LocalBlobStoragePathResolver
    {
        public const string DefaultLocalPath = "Images";
        public const string DefaultPublicPath = "Images";

        public static string ResolveRootPath(IConfiguration configuration)
        {
            var configuredRootPath = configuration["BlobStorage:LocalPath"];
            var rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
                ? DefaultLocalPath
                : configuredRootPath.Trim();

            return Path.GetFullPath(
                Path.IsPathFullyQualified(rootPath)
                    ? rootPath
                    : Path.Combine(AppContext.BaseDirectory, rootPath));
        }

        public static string ResolvePublicPath(IConfiguration configuration)
        {
            return NormalizePublicPath(configuration["BlobStorage:PublicPath"] ?? DefaultPublicPath);
        }

        private static string NormalizePublicPath(string publicPath)
        {
            return string.IsNullOrWhiteSpace(publicPath)
                ? string.Empty
                : publicPath.Trim().Replace('\\', '/').Trim('/');
        }
    }
}
