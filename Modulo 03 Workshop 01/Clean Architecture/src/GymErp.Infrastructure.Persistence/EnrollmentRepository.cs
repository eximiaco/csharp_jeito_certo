using GymErp.Application.Abstractions;
using GymErp.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace GymErp.Infrastructure.Persistence;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly SubscriptionsDbContext _context;

    public EnrollmentRepository(SubscriptionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        await _context.Enrollments.AddAsync(enrollment, cancellationToken);
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _context.Enrollments.Update(enrollment);
        return Task.CompletedTask;
    }
}
