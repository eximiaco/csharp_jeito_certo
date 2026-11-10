using GymErp.Core.Ports.Driven;
using GymErp.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Adapters.Postgres;

public sealed class PostgresEnrollments(SubscriptionsDbContext context) : IForStoringEnrollments
{
    public Task<Enrollment?> FindAsync(Guid enrollmentId, CancellationToken cancellationToken = default) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

    // O commit é detalhe deste adaptador: o núcleo só pede "guarde".
    public async Task SaveAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        if (context.Entry(enrollment).State == EntityState.Detached)
            context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(cancellationToken);
    }
}
