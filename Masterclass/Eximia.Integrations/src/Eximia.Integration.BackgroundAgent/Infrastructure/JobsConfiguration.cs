namespace Eximia.Integration.BackgroundAgent.Infrastructure;

/// <summary>
/// Registers the recurring jobs of the agent. No jobs are registered at the moment.
/// </summary>
public class JobsConfiguration : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // To register a recurring job, inject IRecurringJobManager through the constructor and call:
        // recurringJobManager.AddOrUpdate<MyJob>("my-job", x => x.StartAsync(), Cron.Minutely());
        return Task.CompletedTask;
    }
}
