using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Shared.Telemetry;
using Eximia.Integrations.Shared.OpenTelemetry;

namespace Eximia.Integration.BackgroundAgent.Infrastructure;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(Ambient).Assembly)
            .AsClosedTypesOf(typeof(IService<>))
            .InstancePerLifetimeScope();

        builder
            .RegisterAssemblyTypes(typeof(Integrations.Ambient).Assembly)
            .AsClosedTypesOf(typeof(IService<>))
            .InstancePerLifetimeScope();

        builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().InstancePerLifetimeScope();
        builder.RegisterType<TelemetryFactory>().As<ITelemetryFactory>().InstancePerLifetimeScope();
        builder.RegisterType<TelemetryService>().As<ITelemetryService>().InstancePerLifetimeScope();
        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
        builder.RegisterType<PlatformMetrics>().SingleInstance();
        builder.RegisterType<OtelVariables>().SingleInstance();
        builder.RegisterType<DatadogMetricsService>().SingleInstance();
        builder.RegisterType<SquadsSingleton>().SingleInstance();
    }
}
