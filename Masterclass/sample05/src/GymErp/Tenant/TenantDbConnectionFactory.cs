using System.Data.Common;
using GymErp.Common;
using Npgsql;

namespace GymErp.Tenant;

public class TenantDbConnectionFactory(TenantAccessor tenantAccessor) : IDbConnectionFactory
{
    public async Task<DbConnection> Create(CancellationToken cancellationToken)
    {
        return new NpgsqlConnection(tenantAccessor.Tenant.ConnectionString);
    }
}