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

        // 1) Tạo salt random, độ dài tùy config
        // Salt KHÔNG cần bí mật → chỉ cần random & unique
        var salt = RandomNumberGenerator.GetBytes(Pbkdf2PasswordHashingOptions.SaltSize);

        // 2) Derive key bằng PBKDF2
        // - password: mật khẩu
        // - salt: random salt ở trên
        // - iterations: số vòng lặp → giúp chậm
        // - algorithm: SHA256 → chuẩn OWASP
        // - keySize: độ dài key (bytes)
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Pbkdf2PasswordHashingOptions.Iterations,
            hashAlgorithm: Pbkdf2PasswordHashingOptions.HashAlgorithm,
            outputLength: Pbkdf2PasswordHashingOptions.KeySize);

        // 3) Lưu format: version.iterations.salt.hash
        // Tất cả thông tin đều lưu chung → dễ verify, dễ migrate
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
        if (parts.Length != 4)
            // hash không hợp lệ
            return false;

        var version = parts[0];
        if (version != Pbkdf2PasswordHashingOptions.Version)
            // version không hỗ trợ
            return false;

        if (!int.TryParse(parts[1], out var iterations))
        {
            // iterations không hợp lệ
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);

        // 1) Hash lại password user nhập, dùng cùng salt + iterations
        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password: providedPassword,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: Pbkdf2PasswordHashingOptions.HashAlgorithm,
            outputLength: expectedKey.Length
        );

        // 2) Compare constant-time để tránh timing attack
        // Nếu dùng == là lỗi bảo mật DEADLY
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}