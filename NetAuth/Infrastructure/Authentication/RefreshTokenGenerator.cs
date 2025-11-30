using System.Security.Cryptography;
using System.Text;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

/// <summary>
/// Generates cryptographically secure refresh tokens using RandomNumberGenerator.
/// </summary>
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    public RefreshTokenResult GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        var rawToken = Convert.ToBase64String(randomBytes);

        var tokenHash = ComputeTokenHash(rawToken);

        return new RefreshTokenResult(
            RawToken: rawToken,
            TokenHash: tokenHash);
    }

    public string ComputeTokenHash(string rawToken)
    {
        // Can use SHA256/HMACSHA256 with a secret key for additional security
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}