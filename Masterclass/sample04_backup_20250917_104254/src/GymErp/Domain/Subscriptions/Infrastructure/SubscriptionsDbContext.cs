using Microsoft.EntityFrameworkCore;
using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public sealed class SubscriptionsDbContext : DbContext
{
    public DbSet<Enrollment> Enrollments { get; set; }

    public SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Enrollment>(builder =>
        {
            builder.ToTable("Enrollments");
            builder.HasKey(e => e.Id);
            builder.OwnsOne(e => e.Client, client =>
            {
                client.Property(c => c.Cpf).HasColumnName("ClientCpf");
                client.Property(c => c.Name).HasColumnName("ClientName");
                client.Property(c => c.Email).HasColumnName("ClientEmail");
                client.Property(c => c.Phone).HasColumnName("ClientPhone");
                client.Property(c => c.Address).HasColumnName("ClientAddress");
            });
            builder.Property(e => e.RequestDate).HasColumnName("RequestDate");
            builder.Property(e => e.State).HasColumnName("State");
            builder.Property(e => e.SuspensionStartDate).HasColumnName("SuspensionStartDate");
            builder.Property(e => e.SuspensionEndDate).HasColumnName("SuspensionEndDate");
        });
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            foreach (var item in ChangeTracker.Entries())
            {
                if ((item.State == EntityState.Modified || item.State == EntityState.Added)
                    && item.Properties.Any(c => c.Metadata.Name == "LasUpdatedAt"))
                    item.Property("LasUpdatedAt").CurrentValue = DateTime.UtcNow;

                if (item.State == EntityState.Added)
                    if (item.Properties.Any(c => c.Metadata.Name == "CreatedAt") && item.Property("CreatedAt").CurrentValue.GetType() != typeof(DateTime))
                        item.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
            var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            //await _serviceBus.DispatchDomainEventsAsync(this).ConfigureAwait(false);
            return result;
        }
        catch (DbUpdateException e)
        {
            throw new Exception();
        }
        catch (Exception)
        {
            throw;
        }
    }
}