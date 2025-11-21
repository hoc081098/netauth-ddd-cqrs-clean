namespace NetAuth.Domain.Users;

public interface IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    public void Insert(User user);

    public Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default);
}