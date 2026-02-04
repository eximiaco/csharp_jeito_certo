using GymErp.Common;
using Microsoft.EntityFrameworkCore;
using GymErp.Domain.Financial.Features.Payments.Domain;

namespace GymErp.Domain.Financial.Infrastructure;

public sealed class FinancialDbContext : DbContext
{
    private readonly IServiceBus _serviceBus;

    public DbSet<Payment> Payments { get; set; }

    public FinancialDbContext(DbContextOptions<FinancialDbContext> options, IServiceBus serviceBus) : base(options)
    {
        _serviceBus = serviceBus;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.EnrollmentId).HasColumnName("EnrollmentId");
            builder.Property(p => p.Amount).HasColumnName("Amount");
            builder.Property(p => p.Currency).HasColumnName("Currency");
            builder.Property(p => p.Status).HasColumnName("Status");
            builder.Property(p => p.CreatedAt).HasColumnName("CreatedAt");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            foreach (var item in ChangeTracker.Entries())
            {
                if (item.State == EntityState.Added
                    && item.Properties.Any(c => c.Metadata.Name == "CreatedAt")
                    && item.Property("CreatedAt").CurrentValue is not DateTime)
                {
                    item.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
            var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await _serviceBus.DispatchDomainEventsAsync(this).ConfigureAwait(false);
            return result;
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
