namespace GymErp.Common;

public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}