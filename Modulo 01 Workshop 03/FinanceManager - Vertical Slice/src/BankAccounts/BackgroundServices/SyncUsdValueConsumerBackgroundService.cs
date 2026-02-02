using FinanceManager.Crosscutting.Database;
using FinanceManager.Crosscutting;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using FinanceManager.Crosscutting.ValueObjects;

namespace FinanceManager.BankAccounts.BackgroundServices;

public class SyncUsdValueConsumerBackgroundService(
    Channel<string> channel,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SyncUsdValueFallback> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var transactionId in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
                var exchangeService = scope.ServiceProvider.GetRequiredService<ExchangeService>();

                var transaction = await context.Set<Transaction>().FirstAsync(t => t.Id == transactionId, stoppingToken);
                var usdValueResult = await exchangeService.GetUsdValueAsync(transaction.Value);
                if (!usdValueResult.TryGetValue(out decimal usdValue))
                {
                    logger.LogError("Error getting usd value for transaction {TransactionId}. Errors: {Errors}", transaction.Id, usdValueResult.Errors);
                    continue;
                }

                if (!transaction.SetUsdValue(usdValue).Success)
                {
                    logger.LogError("Error setting usd value for transaction {TransactionId}. Errors: {Errors}", transaction.Id, transaction.SetUsdValue(usdValue).Errors);
                    continue;
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing transaction {TransactionId}", transactionId);
            }
        }
    }
}
