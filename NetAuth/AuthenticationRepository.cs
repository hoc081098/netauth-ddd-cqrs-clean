namespace NetAuth;

public interface IAuthenticationRepository
{
    Task<User> Register(string email, string password, CancellationToken cancellationToken = default);

    Task<User> Login(string email, string password, CancellationToken cancellationToken = default);
}

public sealed class UserNotFoundException(string email)
    : Exception($"User with email '{email}' not found.");

public sealed class UserAlreadyExistsException(string email)
    : Exception($"User with email '{email}' already exists.");

internal sealed class FakeAuthenticationRepository : IAuthenticationRepository
{
    // ---------------------------
    // In-memory fake DB
    // ---------------------------
    private readonly List<User> _users = [];

    public async Task<User> Register(string email, string password, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async DB call

        if (_users.Find(u => u.Email == email) is not null)
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
        var user = _users.Find(u =>
                u.Email == email &&
                u.PasswordHash == password // DEMO ONLY
        );
        return user ?? throw new UserNotFoundException(email);
    }
}