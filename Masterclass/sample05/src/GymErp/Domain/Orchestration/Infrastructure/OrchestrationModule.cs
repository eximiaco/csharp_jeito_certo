using Autofac;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;
using GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;
using GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Steps;
using GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Infrastructure;

public class OrchestrationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.MainWorkflow>().AsSelf().SingleInstance();
        builder.RegisterType<AddEnrollmentStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AddEnrollmentCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessPaymentStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessPaymentCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ScheduleEvaluationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ScheduleEvaluationCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<NewEnrollmentEndpoint>().AsSelf().InstancePerLifetimeScope();
        
        // Registra o Workflow de Cancelamento
        builder.RegisterType<GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.MainWorkflow>().AsSelf().SingleInstance();
        builder.RegisterType<CancelEnrollmentStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<CancelEnrollmentCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessRefundStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessRefundCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow.Endpoint>().AsSelf().InstancePerLifetimeScope();
        
        // Registra o EnrollmentOrchestrator
        builder.RegisterType<LegacyAdapter>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ModernizedAdapter>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<PlanService>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<Handler>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<GymErp.Domain.Orchestration.Features.EnrollmentOrchestrator.Endpoint>().AsSelf().InstancePerLifetimeScope();
    }
} 