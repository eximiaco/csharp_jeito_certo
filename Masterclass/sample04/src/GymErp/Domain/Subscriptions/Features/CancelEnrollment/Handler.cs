using CSharpFunctionalExtensions;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<Response>> HandleAsync(Request request)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, CancellationToken.None);
        if (enrollment == null)
            return Result.Failure<Response>("Inscrição não encontrada");

        var cancelResult = enrollment.Cancel();
        if (cancelResult.IsFailure)
            return Result.Failure<Response>(cancelResult.Error);

        await repository.UpdateAsync(enrollment, CancellationToken.None);
        await unitOfWork.Commit();

        return Result.Success(new Response(enrollment.Id, DateTime.UtcNow));
    }
}