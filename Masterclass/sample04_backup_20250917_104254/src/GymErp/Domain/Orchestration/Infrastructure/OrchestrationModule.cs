using Autofac;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;
using GymErp.Domain.Orchestration.Features.NewEnrollmentFlow.Steps;
using WorkflowCore.Interface;

namespace GymErp.Domain.Orchestration.Infrastructure;

public class OrchestrationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MainWorkflow>().AsSelf().SingleInstance();
        builder.RegisterType<AddEnrollmentStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<AddEnrollmentCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessPaymentStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProcessPaymentCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ScheduleEvaluationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ScheduleEvaluationCompensationStep>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<NewEnrollmentEndpoint>().AsSelf().InstancePerLifetimeScope();
    }
} 