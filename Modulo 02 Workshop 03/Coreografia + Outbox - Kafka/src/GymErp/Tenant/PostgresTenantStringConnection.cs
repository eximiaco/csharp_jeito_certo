using GymErp.Common;
using Npgsql;

namespace GymErp.Tenant;

public class PostgresTenantStringConnection(IConfiguration configuration)
{
    public string GetTenantsConfigStringConnection()
    {
        IConfigurationSection section = configuration.GetRequiredSection("DatabaseConnection");
        var databaseConfiguration = section.Get<DatabaseConfiguration>()?? new DatabaseConfiguration("", 0, "", "",
            "", true, true, 200, 0, 15, 300, true);
        var builder = new NpgsqlConnectionStringBuilder()
        {
            Host = databaseConfiguration!.Host,
            Port = databaseConfiguration.Port,
            Username = databaseConfiguration.User,
            Password = databaseConfiguration.Password,
            Database = databaseConfiguration.DatabaseName,
            SslMode = databaseConfiguration.DisableSsl ? SslMode.Disable : SslMode.Allow,
            Pooling = databaseConfiguration.Pooling,
            MinPoolSize = databaseConfiguration.MinPoolSize,
            MaxPoolSize = databaseConfiguration.MaxPoolSize,
            Timeout = databaseConfiguration.Timeout,
            ConnectionIdleLifetime = databaseConfiguration.ConnectionIdleLifetime,
            Multiplexing = databaseConfiguration.Multiplexing
        };
        return builder.ConnectionString;
    }
}