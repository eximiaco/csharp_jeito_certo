using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public class EnrollmentRepository(IEfDbContextAccessor<SubscriptionsDbContext> context)
{
    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        var dbContext = context.Get();
        await dbContext.Enrollments.AddAsync(enrollment, cancellationToken);
    }
} 