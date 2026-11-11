namespace Eximia.Integrations.DispatchersTopics;

public record DispatcherTopics(
    string Pod,
    IEnumerable<string> Topics,
    string Namespace);
