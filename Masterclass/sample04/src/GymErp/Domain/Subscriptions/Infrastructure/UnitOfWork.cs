using GymErp.Common;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public class UnitOfWork(SubscriptionsDbContext context) : IUnitOfWork
{
    public void Commit()
    {
        context.SaveChanges();
    }
}