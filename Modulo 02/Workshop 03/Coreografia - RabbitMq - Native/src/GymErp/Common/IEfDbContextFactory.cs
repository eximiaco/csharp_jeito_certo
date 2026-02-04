using Microsoft.EntityFrameworkCore;

namespace GymErp.Common;

public interface IEfDbContextFactory<T> where T : DbContext
{
    Task<T> CriarAsync(string codigoTenant);
}