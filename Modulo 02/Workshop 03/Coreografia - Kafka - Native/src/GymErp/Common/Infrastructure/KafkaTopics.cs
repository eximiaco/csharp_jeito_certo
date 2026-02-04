namespace GymErp.Common.Infrastructure;

public static class KafkaTopics
{
    public const string EnrollmentEvents = "enrollment-events";
    public const string ChargingProcessedEvents = "charging-processed-events";
    public const string CancelEnrollmentCommands = "cancel-enrollment-commands";

    public const string FinancialModuleGroup = "financial-module";
    public const string SubscriptionsModuleGroup = "subscriptions-module";
}
