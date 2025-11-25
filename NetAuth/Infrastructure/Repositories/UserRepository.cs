using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class UserRepository(AppDbContext dbContext) :
    GenericRepository<Guid, User>(dbContext),
    IUserRepository
{
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        EntitySet.AsNoTracking()
            .Where(u => u.Email.Value == email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default) =>
        !await EntitySet
            .AsNoTracking()
            .AnyAsync(u => u.Email.Value == email, cancellationToken);
}