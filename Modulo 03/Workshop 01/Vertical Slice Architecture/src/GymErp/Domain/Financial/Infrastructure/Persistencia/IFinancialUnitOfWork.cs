namespace GymErp.Domain.Financial.Infrastructure.Persistencia;

public interface IFinancialUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}
