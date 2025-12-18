namespace NetAuth.Domain.Core.Primitives;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : IEquatable<TId>
{
    public TId Id { get; } = default!;

    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected Entity()
    {
    }

    protected Entity(TId id)
        : this() =>
        Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Entity<TId>)obj);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Transient entities (Id empty) are never equal
        if (Id is Guid id && other.Id is Guid otherId)
        {
            if (id == Guid.Empty || otherId == Guid.Empty) return false;
        }

        return Id.Equals(other.Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}