using Autofac;
using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;
using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters;
using GymErp.Domain.Financial.Features.Payments.Domain;
using GymErp.Domain.Financial.Features.Payments.Services;
using GymErp.Domain.Financial.Infrastructure.Gateways;
using GymErp.Domain.Financial.Infrastructure.Persistencia;

namespace GymErp.Domain.Financial.Infrastructure;

public class FinancialModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<FakePaymentProviderClient>()
            .As<IPaymentProviderClient>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PaymentChargeSemanticService>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<PaymentRepository>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<FinancialUnitOfWork>()
            .As<IFinancialUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterType<Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<EnrollmentCreatedEventSubscriber>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters.Endpoint>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
