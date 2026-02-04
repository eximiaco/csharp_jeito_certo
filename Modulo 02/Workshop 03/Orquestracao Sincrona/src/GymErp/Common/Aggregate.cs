namespace GymErp.Common;

public interface IDomainEvent
{ }

public abstract class Aggregate
{
    private List<IDomainEvent> _domainEvents= [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    internal void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents ??= [];
        _domainEvents.Add(eventItem);
    }

    internal void RemoveDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Remove(eventItem);
    }

    internal void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}