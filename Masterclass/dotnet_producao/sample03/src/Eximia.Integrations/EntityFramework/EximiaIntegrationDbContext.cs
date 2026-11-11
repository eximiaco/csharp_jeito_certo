using Microsoft.EntityFrameworkCore;
using Eximia.Integrations.EntityFramework.EntitiesConfiguration;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.Outbox.EntityFramework;

public class EximiaIntegrationDbContext : DbContext
{
    public EximiaIntegrationDbContext(DbContextOptions options) : base(options)
    { }

    public DbSet<IntegrationEvent> IntegrationEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new IntegrationEventEfMap());
    }
}
