using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public class EnrollmentRepository(IEfDbContextAccessor<SubscriptionsDbContext> context)
{
    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        var dbContext = context.Get();
        await dbContext.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var dbContext = context.Get();
        return await dbContext.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        var dbContext = context.Get();
        dbContext.Enrollments.Update(enrollment);
        await Task.CompletedTask;
    }
} 