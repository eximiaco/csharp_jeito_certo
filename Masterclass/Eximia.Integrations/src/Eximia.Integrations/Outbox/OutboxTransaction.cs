using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Silverback.Messaging.Publishing;
using Silverback.Storage;

namespace Eximia.Integrations.Outbox;

/// <summary>
///     Abre uma transação do EF Core e a "enlista" no <see cref="IPublisher" /> do Silverback, de forma que a gravação
///     no outbox (tabela do Silverback) e as alterações do <see cref="DbContext" /> sejam confirmadas atomicamente.
///     Na v5 do Silverback isso não é mais automático.
/// </summary>
public sealed class OutboxTransaction : IAsyncDisposable
{
    private readonly IDbContextTransaction _transaction;
    private readonly IStorageTransaction _storageTransaction;

    private OutboxTransaction(IDbContextTransaction transaction, IStorageTransaction storageTransaction)
    {
        _transaction = transaction;
        _storageTransaction = storageTransaction;
    }

    public static async Task<OutboxTransaction> BeginAsync(
        DbContext dbContext,
        IPublisher publisher,
        CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // ownTransaction: false -> o commit/dispose da transação continua sendo do EF Core.
        var storageTransaction = publisher.EnlistDbTransaction(transaction.GetDbTransaction(), false);

        return new OutboxTransaction(transaction, storageTransaction);
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        if (_storageTransaction is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else if (_storageTransaction is IDisposable disposable)
            disposable.Dispose();

        await _transaction.DisposeAsync().ConfigureAwait(false);
    }
}
