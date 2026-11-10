using GymErp.Core.Ports.Driven;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Adapters.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Subscriptions") ?? "Host=localhost;Database=GymErp;Username=postgres;Password=postgres";

        services.AddDbContext<SubscriptionsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IForStoringEnrollments, PostgresEnrollments>();

        return services;
    }
}
