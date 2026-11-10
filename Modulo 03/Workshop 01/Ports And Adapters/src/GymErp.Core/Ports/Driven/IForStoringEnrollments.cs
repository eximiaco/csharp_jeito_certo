using GymErp.Domain.Enrollments;

namespace GymErp.Core.Ports.Driven;

// Porta conduzida, no vocabulário do núcleo: guardar e recuperar matrículas.
// Não há IUnitOfWork aqui: como e quando fazer commit é decisão do adaptador.
// Se um caso de uso precisar gravar dois agregados juntos, a transação volta a ser uma porta.
public interface IForStoringEnrollments
{
    Task<Enrollment?> FindAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task SaveAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
}
