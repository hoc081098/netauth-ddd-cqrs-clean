using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

/// <summary>
/// Generates cryptographically secure refresh tokens using RandomNumberGenerator.
/// </summary>
internal sealed class RefreshTokenGenerator(
    IOptions<JwtConfig> jwtConfigOptions
) : IRefreshTokenGenerator
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

    public TimeSpan RefreshTokenExpiration => jwtConfigOptions.Value.RefreshTokenExpiration;
}