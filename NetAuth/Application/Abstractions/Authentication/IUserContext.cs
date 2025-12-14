namespace NetAuth.Application.Abstractions.Authentication;

/// <summary>
/// Provides information about the current user.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the identifier of current authenticated user.
    /// </summary>
    Guid UserId { get; }
    
    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}