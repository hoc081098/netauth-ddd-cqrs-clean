using System.Security.Cryptography;
using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.Infrastructure.Authentication;

/// <summary>
/// Generates cryptographically secure refresh tokens using RandomNumberGenerator.
/// </summary>
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 64;

    public string GenerateToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

