namespace GymErp.Domain.Financial.Infrastructure;

public interface IFinancialUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}
