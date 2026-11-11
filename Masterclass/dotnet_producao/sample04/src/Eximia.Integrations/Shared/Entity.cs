namespace Eximia.Integrations.Shared;

public abstract class Entity<TKey> : IEquatable<Entity<TKey>>, IComparable<Entity<TKey>> where TKey : IComparable
{
    protected Entity() { }

    protected Entity(TKey id)
    {
        if (id is null || id.Equals(default(TKey)))
            throw new ArgumentException("Id cannot be null or default", nameof(id));

        Id = id;
    }

    public TKey Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> compareTo)
            return false;

        if (ReferenceEquals(this, compareTo))
            return true;

        if (Id is null || compareTo.Id is null)
            return false;

        return Id.Equals(compareTo.Id) && GetType() == compareTo.GetType();
    }

    public bool Equals(Entity<TKey>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id) && GetType() == other.GetType();
    }

    public static bool operator ==(Entity<TKey>? a, Entity<TKey>? b)
        => a?.Equals(b) ?? b is null;

    public static bool operator !=(Entity<TKey>? a, Entity<TKey>? b)
        => !(a == b);

    public override int GetHashCode()
        => Id is null ? 0 : HashCode.Combine(GetType(), Id);

    public override string ToString()
        => $"{GetType().Name} [Id={Id}]";

    public virtual int CompareTo(Entity<TKey>? other)
        => other is null ? 1 : Id.CompareTo(other.Id);
}