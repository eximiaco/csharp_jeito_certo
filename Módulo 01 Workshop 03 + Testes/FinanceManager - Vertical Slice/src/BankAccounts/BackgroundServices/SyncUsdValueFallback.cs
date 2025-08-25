using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.BackgroundServices;

public class SyncUsdValueFallback(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SyncUsdValueFallback> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
                var exchangeService = scope.ServiceProvider.GetRequiredService<ExchangeService>();

                var transactions = await context.Set<Transaction>()
                    .Where(t => t.UsdValue.Amount == 0)
                    .ToListAsync(stoppingToken);

                foreach (var transaction in transactions)
                {
                    var usdValueResult = await exchangeService.GetUsdValueAsync(transaction.Value);
                    if (!usdValueResult.TryGetValue(out decimal usdValue))
                    {
                        logger.LogError("Error getting usd value for transaction {TransactionId}. Errors: {Errors}", transaction.Id, usdValueResult.Errors);
                        continue;
                    }

                    transaction.SetUsdValue(usdValue);

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing transactions");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
