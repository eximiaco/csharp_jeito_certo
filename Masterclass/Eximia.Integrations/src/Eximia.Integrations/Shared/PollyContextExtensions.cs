using Polly;

namespace Eximia.Integrations.Shared;

public static class PollyContextExtensions
{
    
    private const string IntegrationKey = "IntegrationId";
    
    public static Context WithIntegrationId(this Context context, int integrationId)
    {
        context[IntegrationKey] = integrationId;
        return context;
    }
    
    public static int? GetIntegrationId(this Context context)
    {
        if (context.TryGetValue(IntegrationKey, out var integrationId))
        {
            return integrationId is int id ? id : 0;
        }
        return null;
    }
}