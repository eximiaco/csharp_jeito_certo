using CSharpFunctionalExtensions;
using GymErp.Domain.Subscriptions.Features.Enrollments.Domain;

namespace GymErp.Domain.Subscriptions.Features.Enrollments.Domain.States;

public interface IEnrollmentState
{
    EState CurrentState { get; }
    Result Activate(Enrollment enrollment);
    Result Suspend(Enrollment enrollment);
    Result Cancel(Enrollment enrollment);
}
