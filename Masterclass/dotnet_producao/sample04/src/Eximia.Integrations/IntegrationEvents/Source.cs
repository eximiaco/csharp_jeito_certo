namespace Eximia.Integrations.IntegrationEvents;

public record Source
{
    public Source(string? name, string? key)
    {
        Name = name;
        Key = key;
    }

    public string? Name { get; }
    public string? Key { get; }
}