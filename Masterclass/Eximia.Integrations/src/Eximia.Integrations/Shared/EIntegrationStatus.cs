namespace Eximia.Integrations.Shared;

public enum EIntegrationStatus
{
    InOutbox = 0,
    Processing = 1,
    WaitingCallback = 2,
    Processed = 3,
    Restarted = 5,
    Reprocessed = 6,
    Error = 9
}
