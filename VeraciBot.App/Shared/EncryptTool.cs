using System.Security.Cryptography;
using System.Text;

namespace VeraciBot.App.Shared
{
    public static class EncryptTool
    {
        private const string CurrentCipherPrefix = "v2:";
        private const string EnvironmentKeyName = "VERACIBOT_ENCRYPTION_KEY";
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private static byte[] configuredCryptoKey;

        public static void Configure(string cryptoKey)
        {
            if (!string.IsNullOrWhiteSpace(cryptoKey))
                configuredCryptoKey = ParseCryptoKey(cryptoKey);
        }

        public static string Encrypt(this string plainText)
        {
            if (String.IsNullOrWhiteSpace(plainText))
                return plainText;

            var key = GetCryptoKey();
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var payload = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, nonce.Length + tag.Length, cipherBytes.Length);

            return CurrentCipherPrefix + Convert.ToBase64String(payload);
        }

        public static string Encrypt(this long plainText)
        {
            return plainText.ToString().Encrypt();
        }

        public static string Decrypt(this string cipherText)
        {
            if (String.IsNullOrWhiteSpace(cipherText))
                return cipherText;

            if (!cipherText.StartsWith(CurrentCipherPrefix, StringComparison.Ordinal))
                return cipherText;

            var key = GetCryptoKey();

            return TryDecryptCurrent(cipherText, key, out var currentDecrypted) ? currentDecrypted : cipherText;
        }

        private static byte[] GetCryptoKey()
        {
            if (configuredCryptoKey != null)
                return configuredCryptoKey;

            var environmentKey = Environment.GetEnvironmentVariable(EnvironmentKeyName);
            if (!string.IsNullOrWhiteSpace(environmentKey))
                return ParseCryptoKey(environmentKey);

            throw new InvalidOperationException(
                $"Configure uma chave de criptografia em 'Encryption:Key' ou na variável de ambiente '{EnvironmentKeyName}'.");
        }

        private static byte[] ParseCryptoKey(string cryptoKey)
        {
            var trimmedKey = cryptoKey.Trim();

            try
            {
                var base64Key = Convert.FromBase64String(trimmedKey);
                if (IsValidAesKey(base64Key))
                    return base64Key;
            }
            catch (FormatException)
            {
            }

            var utf8Key = Encoding.UTF8.GetBytes(trimmedKey);
            if (IsValidAesKey(utf8Key))
                return utf8Key;

            throw new InvalidOperationException(
                $"A chave de criptografia deve ser Base64 ou texto UTF-8 com 16, 24 ou 32 bytes. Configure 'Encryption:Key' ou '{EnvironmentKeyName}'.");
        }

        private static bool IsValidAesKey(byte[] key)
        {
            return key.Length is 16 or 24 or 32;
        }

        private static bool TryDecryptCurrent(string cipherText, byte[] key, out string plainText)
        {
            plainText = cipherText;

            try
            {
                var payload = Convert.FromBase64String(cipherText[CurrentCipherPrefix.Length..]);
                if (payload.Length <= NonceSize + TagSize)
                    return false;

                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var cipherBytes = new byte[payload.Length - NonceSize - TagSize];

                Buffer.BlockCopy(payload, 0, nonce, 0, nonce.Length);
                Buffer.BlockCopy(payload, nonce.Length, tag, 0, tag.Length);
                Buffer.BlockCopy(payload, nonce.Length + tag.Length, cipherBytes, 0, cipherBytes.Length);

                var plainBytes = new byte[cipherBytes.Length];
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

                plainText = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string DescriptografarUrlBase64(this string urlCriptografado)
        {
            if (string.IsNullOrEmpty(urlCriptografado))
                return string.Empty;
            try
            {
                // Reverte o escape da URL (equivalente ao decodeURIComponent)
                var base64String = Uri.UnescapeDataString(urlCriptografado);

                // Converte a string Base64 de volta para bytes
                var urlBytes = Convert.FromBase64String(base64String);

                // Converte os bytes para a string original (UTF8)
                return Encoding.UTF8.GetString(urlBytes);
            }
            catch
            {
                return urlCriptografado;
            }
        }

        public static string CriptografarUrlBase64(this object url)
        {
            if (url == null)
                return string.Empty;

            // Converte o valor para string
            var urlString = url.ToString();

            // Converte a string para bytes (UTF8)
            var urlBytes = Encoding.UTF8.GetBytes(urlString);

            // Converte os bytes para uma string Base64
            var base64String = Convert.ToBase64String(urlBytes);

            // Aplica o escape para URL (equivalente ao encodeURIComponent)
            return Uri.EscapeDataString(base64String);
        }

        public static string GenerateToken(int size = 32)
        {
            var bytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            // Codifica em Base64 URL-safe (sem padding)
            return Convert.ToBase64String(bytes)
                          .Replace("+", "-")
                          .Replace("/", "_")
                          .Replace("=", ""); // Remove o padding opcional
        }
    }
}
