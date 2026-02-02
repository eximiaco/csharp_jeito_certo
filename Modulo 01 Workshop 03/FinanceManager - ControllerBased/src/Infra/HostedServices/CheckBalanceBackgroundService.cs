namespace FinanceManager.Infra.HostedServices;

public class CheckBalanceBackgroundService(IServiceProvider serviceProvider): BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IdentityUserService>();
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    } 
}