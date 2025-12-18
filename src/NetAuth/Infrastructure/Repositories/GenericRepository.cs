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

    /// <summary>
    /// Get entity by id asynchronously.
    /// The returned entity is tracked by the DbContext.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        EntitySet.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken: cancellationToken);

    /// <summary>
    /// Finds an entity with the given primary key values.
    /// If an entity with the given primary key values is being tracked by the context, then it is returned immediately
    /// without making a request to the database.
    /// Otherwise, a query is made to the database for an entity with the given primary key values and this entity,
    /// if found, is attached to the context and returned. If no entity is found, then null is returned.
    /// </summary>
    /// <param name="keyValues"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public ValueTask<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken = default) =>
        EntitySet.FindAsync(keyValues, cancellationToken);

    public void Insert(TEntity entity) =>
        EntitySet.Add(entity);

    public void InsertRange(IEnumerable<TEntity> entities) =>
        EntitySet.AddRange(entities);

    public void Remove(TEntity entity) => EntitySet.Remove(entity);
}