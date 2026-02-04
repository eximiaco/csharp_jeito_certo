using CSharpFunctionalExtensions;
using GymErp.Domain.Subscriptions.Features.Enrollments.Domain;

namespace GymErp.Domain.Subscriptions.Features.Enrollments.Domain.States;

public class CanceledState : IEnrollmentState
{
    public EState CurrentState => EState.Canceled;

    public Result Activate(Enrollment enrollment)
    {
        return Result.Failure("Não é possível ativar uma inscrição cancelada");
    }

    public Result Suspend(Enrollment enrollment)
    {
        return Result.Failure("Não é possível suspender uma inscrição cancelada");
    }

    public Result Cancel(Enrollment enrollment)
    {
        return Result.Failure("Inscrição já está cancelada");
    }
}
