namespace NetAuth.Application.Abstractions.Authentication;

/// <summary>
/// Represents the user identifier provider interface.
/// </summary>
public interface IUserIdentifierProvider
{
    /// <summary>
    /// Gets the identifier of current authenticated user.
    /// </summary>
    Guid UserId { get; }
}