namespace GymErp.Common.Settings;

public record ServicesSettings
{
    public string SubscriptionsUri { get; init; } = string.Empty;
    public string ProcessPaymentUri { get; init; } = string.Empty;
    public string ScheduleEvaluationUri { get; init; } = string.Empty;
}
