using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Cryptography;

internal sealed class Pbkdf2PasswordHasher(ILogger<Pbkdf2PasswordHasher> logger) :
    IPasswordHasher,
    IPasswordHashChecker
{
    public string HashPassword(Password password)
    {
        Guard.Against.NullOrEmpty(password);

        // 1) Generate random salt based on configuration
        // Salt doesn't need to be secret—just random and unique
        var salt = RandomNumberGenerator.GetBytes(Pbkdf2PasswordHashingOptions.SaltSize);

        // 2) Derive key using PBKDF2
        // - password: the password
        // - salt: random salt from above
        // - iterations: number of rounds to slow down hashing
        // - algorithm: SHA256 per OWASP guidance
        // - keySize: key length (bytes)
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Pbkdf2PasswordHashingOptions.Iterations,
            hashAlgorithm: Pbkdf2PasswordHashingOptions.HashAlgorithm,
            outputLength: Pbkdf2PasswordHashingOptions.KeySize);

        // 3) Store format: version.iterations.salt.hash
        // Keep all metadata together for easy verification and migration
        return new StringBuilder()
            .Append(Pbkdf2PasswordHashingOptions.Version)
            .Append(Pbkdf2PasswordHashingOptions.Delimiter)
            .Append(Pbkdf2PasswordHashingOptions.Iterations)
            .Append(Pbkdf2PasswordHashingOptions.Delimiter)
            .Append(Convert.ToBase64String(salt))
            .Append(Pbkdf2PasswordHashingOptions.Delimiter)
            .Append(Convert.ToBase64String(key))
            .ToString();
    }

    public bool IsMatch(string passwordHash, string providedPassword)
    {
        Guard.Against.NullOrEmpty(passwordHash);
        Guard.Against.NullOrEmpty(providedPassword);

        // Format: version.iterations.salt.hash
        var parts = passwordHash.Split(Pbkdf2PasswordHashingOptions.Delimiter,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
        {
            // invalid format
            logger.LogWarning("Password hash format invalid. Expected 4 parts.");
            return false;
        }

        if (parts[0] != Pbkdf2PasswordHashingOptions.Version)
        {
            // unsupported version
            // TODO: handle other versions (v2 = Argon2, etc.)
            logger.LogWarning("Password hash version {PasswordHashVersion} is not supported.", parts[0]);
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            // iterations value is invalid
            logger.LogWarning("Invalid iterations value in password hash: {PasswordIterationsValue}", parts[1]);
            return false;
        }

        byte[] salt;
        byte[] expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedKey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException exception)
        {
            logger.LogWarning(exception, "Failed to parse Base64 strings from password hash.");
            return false;
        }

        // 1) Rehash provided password using same salt and iterations
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password: providedPassword,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: Pbkdf2PasswordHashingOptions.HashAlgorithm,
            outputLength: expectedKey.Length
        );

        // 2) Compare in constant time to avoid timing attacks
        // Using == would be a critical vulnerability
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}