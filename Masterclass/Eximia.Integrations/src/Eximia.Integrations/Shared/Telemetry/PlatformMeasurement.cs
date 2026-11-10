namespace Eximia.Integrations.Shared.Telemetry;

public record PlatformMeasurement(long Total, string? Origin, string? Destiny, string? Entity, string? Squad, bool IsCallback)
{
    public long Total { get; private set; } = Total;
    public string? Squad { get; private set; } = Squad;
    public bool IsCallback { get; } = IsCallback;

    public void Reset() => Total = 0;

    public void Update(long newValue)
    {
        Total = newValue;
    }

    public void UpdateSquad(string? squad)
    {
        Squad = squad;
    }

    public static PlatformMeasurement Create(string? origin, string? destiny, string? entity, string? squad, bool isCallback)
        => new PlatformMeasurement(1, origin, destiny, entity, squad, isCallback);
}
