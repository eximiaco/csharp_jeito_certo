using GymErp.Domain.Enrollments;

namespace GymErp.Application.Enrollments;

public interface IEnrollmentRepository
{
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}
