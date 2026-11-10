using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.DomainServices;

public class RouteValidator : IService<RouteValidator>
{
    private readonly IConfiguration _configuration;

    public RouteValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<bool> RouteIsValid(string destiny, string entity)
    {
        await using var connection = new NpgsqlConnection(_configuration.GetConnectionString(Ambient.DatabaseConnectionName));
        var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT id FROM destination_routes WHERE destination = @destiny and entity = @entity", 
            new {destiny, entity});
        return result > 0;
    }
}
