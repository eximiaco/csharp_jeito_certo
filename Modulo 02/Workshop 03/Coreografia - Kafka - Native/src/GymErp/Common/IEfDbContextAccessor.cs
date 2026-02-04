using Microsoft.EntityFrameworkCore;

namespace GymErp.Common;

public interface IEfDbContextAccessor<T> : IDisposable where T : DbContext
{
    void Register(T context);
    T Get();
    void Clear();
}