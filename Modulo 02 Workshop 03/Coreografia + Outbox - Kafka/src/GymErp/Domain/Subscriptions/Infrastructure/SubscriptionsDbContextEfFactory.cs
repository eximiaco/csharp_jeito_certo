using GymErp.Common;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public sealed class SubscriptionsDbContextEfFactory(
    ITenantLocator tenantLocator,
    IServiceBus serviceBus) : IEfDbContextFactory<SubscriptionsDbContext>
{
    public async Task<SubscriptionsDbContext> CriarAsync(string codigoTenant)
    {
        var tenant = await tenantLocator.Get(codigoTenant, CancellationToken.None);
        var optionsBuilder = new DbContextOptionsBuilder<SubscriptionsDbContext>();
        optionsBuilder.UseNpgsql(tenant.ConnectionString);
        return new SubscriptionsDbContext(optionsBuilder.Options, serviceBus);
    }
}
