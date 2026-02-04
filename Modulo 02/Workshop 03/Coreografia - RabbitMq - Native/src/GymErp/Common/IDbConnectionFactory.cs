using System.Data.Common;

namespace GymErp.Common;

public interface IDbConnectionFactory
{
    Task<DbConnection> Create(CancellationToken cancellationToken);
}