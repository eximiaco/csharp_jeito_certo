using CSharpFunctionalExtensions;
using GymErp.Application.Ports.Inbound;
using GymErp.Application.Ports.Outbound;

namespace GymErp.Application.UseCases.SuspendEnrollment;

public class SuspendEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : ISuspendEnrollmentUseCase
{
    public async Task<Result<bool>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            return Result.Failure<bool>("Período de suspensão inválido. Mínimo 30 dias.");

        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return Result.Failure<bool>("Inscrição não encontrada");

        var result = enrollment.Suspend(request.SuspensionStartDate, request.SuspensionEndDate);
        if (result.IsFailure)
            return Result.Failure<bool>(result.Error);

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return Result.Success(true);
    }
}
