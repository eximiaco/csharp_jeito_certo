using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public class UnitOfWork(SubscriptionsDbContext context) : IUnitOfWork
{
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public void Commit()
    {
        context.SaveChanges();
    }
}