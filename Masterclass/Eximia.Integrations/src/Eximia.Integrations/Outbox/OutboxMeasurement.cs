namespace Eximia.Integrations.Outbox;

public record OutboxMeasurement
{
    public OutboxMeasurement(long total, string? origin, string? destiny, string? entity)
    {
        Total = total;
        Origin = origin;
        Destiny = destiny;
        Entity = entity;
    }

    public long Total { get; private set; }
    public string? Origin { get; }
    public string? Destiny { get; }
    public string? Entity { get; }

    public void Reset() => Total = 0;

    public void Update(long newValue)
    {
        Total = newValue;
    }
}