using Microsoft.EntityFrameworkCore;
using GymErp.Domain.Subscriptions;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public sealed class SubscriptionsDbContext : DbContext
{
    public DbSet<Enrollment> Enrollments { get; set; }

    public SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollments");
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Client, client =>
            {
                client.Property(c => c.Cpf).HasColumnName("Document");
                client.Property(c => c.Name).HasColumnName("Name");
                client.Property(c => c.Email).HasColumnName("Email");
                client.Property(c => c.Phone).HasColumnName("Phone");
                client.Property(c => c.Address).HasColumnName("Address");
            });
            entity.Property(e => e.RequestDate).HasColumnName("RequestDate");
            entity.Property(e => e.State).HasColumnName("State");
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