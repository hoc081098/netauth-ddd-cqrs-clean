using System.Collections.Concurrent;

namespace NetAuth;

public interface IAuthenticationRepository
{
    Task<User> Register(string email, string password, CancellationToken cancellationToken = default);

    Task<User> Login(string email, string password, CancellationToken cancellationToken = default);
}

public sealed class UserNotFoundException(string email)
    : Exception($"User with email '{email}' not found.");

public sealed class WrongPasswordException()
    : Exception("The provided password is incorrect.");

public sealed class UserAlreadyExistsException(string email)
    : Exception($"User with email '{email}' already exists.");

internal sealed class FakeAuthenticationRepository : IAuthenticationRepository
{
    // ---------------------------
    // In-memory fake DB
    // ---------------------------
    private readonly ConcurrentBag<User> _users =
    [
        new(Id: Guid.NewGuid(), Email: "hoc081098@gmail.com", PasswordHash: "123456")
    ];

    public async Task<User> Register(string email, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async DB call

        if (_users.FirstOrDefault(u => u.Email == email) is not null)
        {
            throw new UserAlreadyExistsException(email);
        }

        var user = new User(Id: Guid.NewGuid(),
            Email: email,
            PasswordHash: password // DEMO ONLY
        );
        _users.Add(user);
        return user;
    }

    public async Task<User> Login(string email, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async DB call
        return _users.FirstOrDefault(u => u.Email == email) switch
        {
            null => throw new UserNotFoundException(email),
            var user when user.PasswordHash != password => throw new WrongPasswordException(),
            var user => user
        };
    }
}