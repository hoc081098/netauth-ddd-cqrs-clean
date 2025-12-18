namespace NetAuth.Domain.Core.Primitives;

public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the atomic values of the value object.
    /// </summary>
    /// <returns>The collection of objects representing the value object values.</returns>
    protected abstract IEnumerable<object> GetAtomicValues();

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ValueObject)obj);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = default;
        foreach (var obj in GetAtomicValues())
        {
            hashCode.Add(obj);
        }

        return hashCode.ToHashCode();
    }

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}