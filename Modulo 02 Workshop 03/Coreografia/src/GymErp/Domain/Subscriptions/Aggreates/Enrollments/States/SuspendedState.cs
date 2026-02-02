using CSharpFunctionalExtensions;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments.States;

public class SuspendedState : IEnrollmentState
{
    public EState CurrentState => EState.Suspended;

    public Result Activate(Enrollment enrollment)
    {
        enrollment.ChangeState(EState.Active);
        return Result.Success();
    }

    public Result Suspend(Enrollment enrollment)
    {
        return Result.Failure("Inscrição já está suspensa");
    }

    public Result Cancel(Enrollment enrollment)
    {
        enrollment.ChangeState(EState.Canceled);
        return Result.Success();
    }
} 