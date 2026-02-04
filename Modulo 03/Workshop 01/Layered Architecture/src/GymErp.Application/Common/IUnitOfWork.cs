namespace GymErp.Application.Common;

public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}
