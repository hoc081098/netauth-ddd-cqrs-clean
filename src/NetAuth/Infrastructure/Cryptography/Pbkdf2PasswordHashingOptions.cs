using System.Security.Cryptography;

namespace NetAuth.Infrastructure.Cryptography;

internal static class Pbkdf2PasswordHashingOptions
{
    // Format: version.iterations.salt.hash

    // PBKDF2 iterations → higher means slower but more secure
    internal const int Iterations = 80_000;

    // Salt length (bytes) → 16 bytes = 128 bits is ideal
    internal const int SaltSize = 128 / 8; // 128 bits

    // Final key length (bytes) → 32 bytes = 256 bits
    internal const int KeySize = 256 / 8; // 256 bits

    // HMAC algorithm → SHA256 is the best default for PBKDF2
    internal static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    // Hash version → allows future migration from v1 to v2 (e.g., Argon2)
    internal const string Version = "v1";

    // Delimiter between segments of the hash string
    internal const char Delimiter = '.';
}