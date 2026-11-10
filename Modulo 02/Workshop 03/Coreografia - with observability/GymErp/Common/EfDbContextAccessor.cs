using Microsoft.EntityFrameworkCore;

namespace GymErp.Common;

/// <summary>
/// Guarda o DbContext "do tenant" no escopo de request para que repositórios/handlers
/// consigam usá-lo depois que o middleware setar o contexto.
/// </summary>
public sealed class EfDbContextAccessor<TDbContext> : IEfDbContextAccessor<TDbContext>
    where TDbContext : DbContext
{
    private TDbContext? _contexto;

    public EfDbContextAccessor() { }

    public EfDbContextAccessor(TDbContext context) => Register(context);

    public void Register(TDbContext context)
    {
        _contexto = context ?? throw new ArgumentNullException(nameof(context));
    }

    public TDbContext Get()
    {
        return _contexto ?? throw new InvalidOperationException("Contexto deve ser registrado!");
    }

    public void Clear()
    {
        if (_contexto is null)
            return;

        _contexto.Dispose();
        _contexto = null!;
    }

    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }
}

