using GymErp.Application.Common;

namespace GymErp.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly SubscriptionsDbContext _context;

    public UnitOfWork(SubscriptionsDbContext context)
    {
        _context = context;
    }

    public async Task Commit(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
