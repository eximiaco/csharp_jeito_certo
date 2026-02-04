using GymErp.Application.UseCases.AddNewEnrollment;
using GymErp.Application.UseCases.CancelEnrollment;
using GymErp.Application.UseCases.SuspendEnrollment;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAddNewEnrollmentUseCase, AddNewEnrollmentHandler>();
        services.AddScoped<ICancelEnrollmentUseCase, CancelEnrollmentHandler>();
        services.AddScoped<ISuspendEnrollmentUseCase, SuspendEnrollmentHandler>();

        return services;
    }
}
