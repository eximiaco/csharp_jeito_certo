using GymErp.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public sealed class SubscriptionsDbContextFactory : IDesignTimeDbContextFactory<SubscriptionsDbContext>
{
    public SubscriptionsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = GetConnectionString(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<SubscriptionsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SubscriptionsDbContext(optionsBuilder.Options, new DesignTimeServiceBus());
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var section = configuration.GetSection("DatabaseConnection");
        var config = section.Get<DatabaseConfiguration>() ?? new DatabaseConfiguration("localhost", 5432, "postgres", "", "gymerp", true, true, 200, 0, 15, 300, true);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = config.Host,
            Port = config.Port,
            Username = config.User,
            Password = config.Password,
            Database = config.DatabaseName,
            SslMode = config.DisableSsl ? SslMode.Disable : SslMode.Allow
        };
        return builder.ConnectionString;
    }

    private sealed class DesignTimeServiceBus : IServiceBus
    {
        public Task PublishAsync(object message) => Task.CompletedTask;
    }
}
