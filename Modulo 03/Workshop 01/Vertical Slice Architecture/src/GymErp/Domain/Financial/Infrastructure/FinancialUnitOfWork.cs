namespace GymErp.Domain.Financial.Infrastructure;

public class FinancialUnitOfWork(FinancialDbContext context) : IFinancialUnitOfWork
{
    public async Task Commit(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
