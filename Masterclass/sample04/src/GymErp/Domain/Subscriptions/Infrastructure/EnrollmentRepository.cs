using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Domain.Subscriptions.Infrastructure;

public class EnrollmentRepository(SubscriptionsDbContext context)
{
    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        await context.Enrollments.AddAsync(enrollment, cancellationToken);
    }
} 