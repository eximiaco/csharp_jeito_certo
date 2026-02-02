using System.Security.Authentication;
using Dapper;
using GymErp.Common;
using Npgsql;

namespace GymErp.Tenant;

public class PostgresTenantLocatorStrategy(PostgresTenantStringConnection postgresTenantStringConnection) : ITenantLocator
{
    
    public async Task<Common.GymErpTenant> Get(string identifier, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgresTenantStringConnection.GetTenantsConfigStringConnection());
        
        Common.GymErpTenant gymErpTenant;
        
        // Verifica se o identificador é um GUID válido
        if (Guid.TryParse(identifier, out Guid tenantId))
        {
            // Pesquisa pelo ID do tenant
            gymErpTenant = await connection.QueryFirstOrDefaultAsync<Common.GymErpTenant>(
                @"select ""Id"", ""Name"", ""ConnectionString"" from public.""Client"" where ""Id"" = @id",
                new { id = tenantId });
        }
        else
        {
            // Pesquisa pelo nome do tenant
            gymErpTenant = await connection.QueryFirstOrDefaultAsync<Common.GymErpTenant>(
                @"select ""Id"", ""Name"", ""ConnectionString"" from public.""Client"" where ""Name"" = @name",
                new { name = identifier });
        }
        
        if (gymErpTenant == null)
            throw new AuthenticationException($"Tenant not found [{identifier}]");
            
        return gymErpTenant;
    }

    public async Task<IEnumerable<Common.GymErpTenant>> GetAll(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgresTenantStringConnection.GetTenantsConfigStringConnection());
        const string query = @"select c.""Id"", c.""Name"", c.""ConnectionString"" from public.""Client"" c where c.""Name"" <> 'default';";
        var tenants = await connection.QueryAsync<Common.GymErpTenant>(query);
        return tenants;
    }
}
