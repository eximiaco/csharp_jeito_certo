using Autofac;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Features.Enrollments.Domain;
using GymErp.Domain.Subscriptions.Infrastructure;

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
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.AddNewEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Nova Inscrição
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.AddNewEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler de Suspensão
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.SuspendEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Suspensão
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.SuspendEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler de Cancelamento
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.CancelEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Cancelamento
        builder.RegisterType<GymErp.Domain.Subscriptions.Features.Enrollments.Application.CancelEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
} 