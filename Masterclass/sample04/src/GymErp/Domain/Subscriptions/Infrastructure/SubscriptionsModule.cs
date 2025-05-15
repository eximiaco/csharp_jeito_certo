using Autofac;
using Gymerp.Domain.Subscriptions.AddNewEnrollment;
using GymErp.Domain.Subscriptions.Infrastructure;
using Endpoint = Gymerp.Domain.Subscriptions.AddNewEnrollment.Endpoint;

namespace Gymerp.Domain.Subscriptions.Infrastructure;

public class SubscriptionsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Registra o DbContext
        builder.RegisterType<SubscriptionsDbContext>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Repositório
        builder.RegisterType<EnrollmentRepository>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler
        builder.RegisterType<Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint
        builder.RegisterType<Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
} 