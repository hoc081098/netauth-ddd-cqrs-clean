namespace NetAuth.Application.Abstractions.Authentication;

/// <summary>
/// Provides functionality to generate secure refresh tokens.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new cryptographically secure refresh token.
    /// </summary>
    /// <returns>A Base64-encoded refresh token string.</returns>
    string GenerateToken();
}

