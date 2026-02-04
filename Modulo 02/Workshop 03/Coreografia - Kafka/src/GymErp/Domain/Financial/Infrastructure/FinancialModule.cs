using Autofac;
using GymErp.Domain.Financial.Features.ProcessCharging;

namespace GymErp.Domain.Financial.Infrastructure;

public class FinancialModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<Handler>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
