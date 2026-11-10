using GymErp.Application.UseCases.AddNewEnrollment;
using GymErp.Application.UseCases.CancelEnrollment;
using GymErp.Application.UseCases.SuspendEnrollment;

namespace GymErp.Api;

// Composition root: o único lugar que liga as interfaces dos casos de uso às implementações.
// Fica no host para que a Application não precise conhecer o container de DI.
public static class CompositionRoot
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IAddNewEnrollmentUseCase, AddNewEnrollmentHandler>();
        services.AddScoped<ICancelEnrollmentUseCase, CancelEnrollmentHandler>();
        services.AddScoped<ISuspendEnrollmentUseCase, SuspendEnrollmentHandler>();
        return services;
    }
}
