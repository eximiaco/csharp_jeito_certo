using GymErp.Domain.Subscriptions;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Gymerp.Domain.Subscriptions.Infrastructure;

public class EnrollmentRepository
{
    private readonly SubscriptionsDbContext _context;

    public EnrollmentRepository(SubscriptionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
} 