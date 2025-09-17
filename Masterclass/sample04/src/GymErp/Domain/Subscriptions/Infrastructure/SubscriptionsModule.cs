using Autofac;
using GymErp.Common;
using GymErp.Domain.Subscriptions.AddNewEnrollment;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;
using GymErp.Domain.Subscriptions.SuspendEnrollment;
using Endpoint = GymErp.Domain.Subscriptions.AddNewEnrollment.Endpoint;

namespace GymErp.Domain.Subscriptions.Infrastructure;

public class SubscriptionsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();
        
        // Registra o DbContext
        builder.RegisterType<SubscriptionsDbContext>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Repositório
        builder.RegisterType<EnrollmentRepository>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler de Nova Inscrição
        builder.RegisterType<Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Nova Inscrição
        builder.RegisterType<Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler de Suspensão
        builder.RegisterType<SuspendEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Suspensão
        builder.RegisterType<SuspendEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
} 