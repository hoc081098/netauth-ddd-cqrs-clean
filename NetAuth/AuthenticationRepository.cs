using System.Collections.Concurrent;

namespace NetAuth;

[Obsolete]
public interface IAuthenticationRepository
{
    Task<LegacyUser> Register(string username, string email, string password,
        CancellationToken cancellationToken = default);

    Task<LegacyUser> Login(string email, string password, CancellationToken cancellationToken = default);
}

[Obsolete]
public sealed class UserNotFoundException(string email)
    : Exception($"LegacyUser with email '{email}' not found.");

[Obsolete]
public sealed class WrongPasswordException()
    : Exception("The provided password is incorrect.");

[Obsolete]
public sealed class UserAlreadyExistsException(string email)
    : Exception($"LegacyUser with email '{email}' already exists.");

[Obsolete]
internal sealed class FakeAuthenticationRepository : IAuthenticationRepository
{
    // ---------------------------
    // In-memory fake DB
    // ---------------------------
    private readonly ConcurrentBag<LegacyUser> _users =
    [
        new(Id: Guid.NewGuid(), Username: "hoc081098", Email: "hoc081098@gmail.com", PasswordHash: "123456")
    ];

    public async Task<LegacyUser> Register(string username, string email, string password,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async DB call

        if (_users.FirstOrDefault(u => u.Email == email) is not null)
        {
            throw new UserAlreadyExistsException(email);
        }

        var user = new LegacyUser(Id: Guid.NewGuid(),
            Username: username,
            Email: email,
            PasswordHash: password // DEMO ONLY
        );
        _users.Add(user);
        return user;
    }

    public async Task<LegacyUser> Login(string email, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async DB call
        return _users.FirstOrDefault(u => u.Email == email) switch
        {
            null => throw new UserNotFoundException(email),
            var user when user.PasswordHash != password // DEMO ONLY
                => throw new WrongPasswordException(),
            var user => user
        };
    }
}