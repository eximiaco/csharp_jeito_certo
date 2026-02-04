using GymErp.Common;
using Microsoft.EntityFrameworkCore;

namespace GymErp.IntegrationTests.Infrastructure;

public class EfDbContextAccessor<TDbContext> : IEfDbContextAccessor<TDbContext> where TDbContext : DbContext
{
    private TDbContext? _dbContext;

    public EfDbContextAccessor(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Register(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public TDbContext Get()
    {
        if (_dbContext == null)
            throw new InvalidOperationException("DbContext não registrado.");
        return _dbContext;
    }

    public void Clear()
    {
        _dbContext = null;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _dbContext = null;
    }

    public TDbContext DbContext => Get();
} 