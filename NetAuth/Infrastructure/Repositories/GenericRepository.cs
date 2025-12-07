using Microsoft.EntityFrameworkCore;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Infrastructure.Repositories;

internal abstract class GenericRepository<TId, TEntity>
    where TEntity : Entity<TId>
    where TId : IEquatable<TId>
{
    protected GenericRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    protected AppDbContext DbContext { get; }
    protected DbSet<TEntity> EntitySet => DbContext.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        EntitySet.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken: cancellationToken);

    public ValueTask<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default) =>
        EntitySet.FindAsync(keyValues, cancellationToken);

    public void Insert(TEntity entity) =>
        EntitySet.Add(entity);

    public void InsertRange(IEnumerable<TEntity> entities) =>
        EntitySet.AddRange(entities);

    public void Update(TEntity entity) => EntitySet.Update(entity);

    public void Remove(TEntity entity) => EntitySet.Remove(entity);
}