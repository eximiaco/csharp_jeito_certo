namespace Eximia.Integrations.Shared;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
