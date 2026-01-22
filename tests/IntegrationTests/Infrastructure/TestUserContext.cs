using NetAuth.Application.Abstractions.Authentication;

namespace NetAuth.IntegrationTests.Infrastructure;

/// <summary>
/// A test-friendly implementation of <see cref="IUserContext"/> that allows setting the current user
/// for integration tests without requiring HTTP authentication.
/// </summary>
public sealed class TestUserContext : IUserContext
{
    private Guid _userId = Guid.Empty;
    private readonly HashSet<string> _permissions = [];

    #region Implementation of IUserContext

    public Guid UserId =>
        !IsAuthenticated ? throw new InvalidOperationException("The user is not authenticated.") : _userId;

    public bool IsAuthenticated { get; private set; }

    public bool HasPermission(string permission) =>
        IsAuthenticated && _permissions.Contains(permission);

    #endregion

    /// <summary>
    /// Sets the current user for the test context.
    /// </summary>
    /// <param name="userId">The user ID to set.</param>
    /// <param name="permissions">Optional permissions to grant to the user.</param>
    public void SetCurrentUser(Guid userId, params string[] permissions)
    {
        _userId = userId;
        IsAuthenticated = true;

        _permissions.Clear();
        foreach (var permission in permissions)
        {
            _permissions.Add(permission);
        }
    }

    /// <summary>
    /// Clears the current user, simulating an unauthenticated state.
    /// </summary>
    public void ClearCurrentUser()
    {
        _userId = Guid.Empty;
        IsAuthenticated = false;
        _permissions.Clear();
    }
}