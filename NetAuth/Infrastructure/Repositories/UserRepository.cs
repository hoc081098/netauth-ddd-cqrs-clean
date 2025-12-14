using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Users;

namespace NetAuth.Infrastructure.Repositories;

internal sealed class UserRepository(AppDbContext dbContext) :
    GenericRepository<Guid, User>(dbContext),
    IUserRepository
{
    public Task<User?> GetByIdAsyncWithRoles(Guid id, CancellationToken cancellationToken = default) =>
        EntitySet
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        EntitySet.AsNoTracking()
            .Where(u => u.Email.Value == email)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> IsEmailUniqueAsync(Email email, CancellationToken cancellationToken = default) =>
        !await EntitySet
            .AsNoTracking()
            .AnyAsync(u => u.Email.Value == email, cancellationToken);

    public new void Insert(User user)
    {
        foreach (var role in user.Roles)
        {
            DbContext.Attach(role);
        }

        base.Insert(user);
    }
}