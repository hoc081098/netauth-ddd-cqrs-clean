using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using NetAuth.Application.Abstractions.Cryptography;
using NetAuth.Domain.Users;

namespace NetAuth.Data.Cryptography;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher, IPasswordHashChecker
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
        var parts = passwordHash.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts is not [Pbkdf2PasswordHashingOptions.Version, _, _, _])
        {
            // invalid format or unsupported version
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            // iterations value is invalid
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);

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