using GymErp.Application.Enrollments.AddNewEnrollment;
using GymErp.Application.Enrollments.CancelEnrollment;
using GymErp.Application.Enrollments.SuspendEnrollment;
using Microsoft.Extensions.DependencyInjection;

namespace GymErp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAddNewEnrollmentService, AddNewEnrollmentHandler>();
        services.AddScoped<ICancelEnrollmentService, CancelEnrollmentHandler>();
        services.AddScoped<ISuspendEnrollmentService, SuspendEnrollmentHandler>();

        return services;
    }
}
