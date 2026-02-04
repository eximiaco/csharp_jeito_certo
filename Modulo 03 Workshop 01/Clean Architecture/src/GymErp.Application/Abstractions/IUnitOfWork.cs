namespace GymErp.Application.Abstractions;

public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}
