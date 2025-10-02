namespace GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;

public class LegacyApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 300;
    public int RetryAttempts { get; set; } = 3;
}

public class SubscriptionsApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
