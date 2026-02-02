using CSharpFunctionalExtensions;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments.States;

public class ActiveState : IEnrollmentState
{
    public EState CurrentState => EState.Active;

    public Result Activate(Enrollment enrollment)
    {
        return Result.Failure("Inscrição já está ativa");
    }

    public Result Suspend(Enrollment enrollment)
    {
        enrollment.ChangeState(EState.Suspended);
        return Result.Success();
    }

    public Result Cancel(Enrollment enrollment)
    {
        enrollment.ChangeState(EState.Canceled);
        return Result.Success();
    }
} 