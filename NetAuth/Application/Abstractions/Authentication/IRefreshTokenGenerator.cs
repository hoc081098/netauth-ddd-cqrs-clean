namespace NetAuth.Application.Abstractions.Authentication;

public readonly record struct RefreshTokenResult(
    string RawToken,
    string TokenHash);

/// <summary>
/// Provides functionality to generate secure refresh tokens.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a new cryptographically secure refresh token.
    /// </summary>
    /// <returns>A Base64-encoded refresh token string.</returns>
    RefreshTokenResult GenerateRefreshToken();
    
    string ComputeTokenHash(string rawToken);
}