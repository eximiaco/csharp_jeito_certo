namespace Eximia.Integrations.IntegrationEvents.Commands
{
    public record ProcessIntegrationEventCallbackCommand(int Id, int StatusCode, string Key);
}
