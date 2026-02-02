namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments.States;

public static class EnrollmentStateFactory
{
    public static IEnrollmentState CreateState(EState state)
    {
        return state switch
        {
            EState.Active => new ActiveState(),
            EState.Suspended => new SuspendedState(),
            EState.Canceled => new CanceledState(),
            _ => throw new ArgumentException($"Estado inválido: {state}")
        };
    }
} 