using Autofac;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;
using Endpoint = GymErp.Domain.Subscriptions.Features.AddNewEnrollment.Endpoint;
using Handler = GymErp.Domain.Subscriptions.Features.AddNewEnrollment.Handler;

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
        builder.RegisterType<Features.SuspendEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Suspensão
        builder.RegisterType<Features.SuspendEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Handler de Cancelamento
        builder.RegisterType<Features.CancelEnrollment.Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Cancelamento
        builder.RegisterType<Features.CancelEnrollment.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Registra o Endpoint de Consulta de Plano
        builder.RegisterType<Features.GetPlanById.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
} 