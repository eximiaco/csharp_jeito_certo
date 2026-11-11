using Microsoft.Extensions.Hosting;

namespace Eximia.Integrations.Shared.Kafka;

public sealed class TopicNameFactoryPeerEnviroment
{
    private readonly IHostEnvironment _environment;

    public TopicNameFactoryPeerEnviroment(IHostEnvironment environment)
    {
        _environment = environment;
    }

    private const string productName = "integration-platform";

    public string CreateForDestiny(string destiny, string entity)
        => $"{GetEnviroment()}.{productName}.{destiny.ToLower()}-{entity.ToLower()}";

    public string Create(string topic)
        => $"{GetEnviroment()}.{productName}.{topic}";

    private string GetEnviroment()
    {
        var env = "dev";
        if (_environment.IsStaging() || _environment.IsEnvironment("qas")) env = "stg";
        if (_environment.IsProduction()) env = "prd";
        if (_environment.IsEnvironment("qas-410")) env = "qas";
        return env;
    }
}
