using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Eximia.Integrations.Shared.Postgres;

public class PostgresConnectionStringFactory
{
    private readonly DatabaseConfiguration _configuration;

    public PostgresConnectionStringFactory(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetRequiredSection("DatabaseConnection");
        _configuration = section.Get<DatabaseConfiguration>()?? new DatabaseConfiguration("", 0, "", "",
            "", true, true, 200, 0, 15, 300, true);
    }

    public NpgsqlConnectionStringBuilder CreateConnectionString()
    {
        return new NpgsqlConnectionStringBuilder()
        {
            Host = _configuration!.Host,
            Port = _configuration.Port,
            Username = _configuration.User,
            Password = _configuration.Password,
            Database = _configuration.DatabaseName,
            SslMode = _configuration.DisableSsl ? SslMode.Disable : SslMode.Allow,
            Pooling = _configuration.Pooling,
            MinPoolSize = _configuration.MinPoolSize,
            MaxPoolSize = _configuration.MaxPoolSize,
            Timeout = _configuration.Timeout,
            ConnectionIdleLifetime = _configuration.ConnectionIdleLifetime,
            Multiplexing = _configuration.Multiplexing,
            Encoding = "UTF8"
        };
    }
}
