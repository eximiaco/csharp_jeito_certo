namespace GymErp.Application.Ports.Outbound;

public interface IUnitOfWork
{
    Task Commit(CancellationToken cancellationToken = default);
}
