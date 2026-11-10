using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Shared.Telemetry;
using Eximia.Integrations.IntegrationEvents.Telemetry;
using Eximia.Integrations.IntegrationEvents.Telemetry.Callback;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.EventDispatcher.Infrastructure;

public class ApplicationModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(Ambient).Assembly)
            .AsClosedTypesOf(typeof(IService<>))
            .InstancePerLifetimeScope();

        builder.RegisterType<HttpContextAccessor>().As<IHttpContextAccessor>().InstancePerLifetimeScope();
        builder.RegisterType<IntegrationRequestHttpAdapter>().As<IIntegrationRequest>().InstancePerLifetimeScope();
        builder.RegisterType<EximiaProblemDetailsFactory>().As<EximiaProblemDetailsFactory>().SingleInstance();
        builder.RegisterType<TelemetryFactory>().As<ITelemetryFactory>().InstancePerLifetimeScope();
        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
        builder.RegisterType<RegisterNewIntegrationEventOtelTelemetry>().As<IRegisterNewIntegrationEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<RouteIntegrationEventOtelTelemetry>().As<IRouteIntegrationEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<DispatcherEventTelemetry>().As<IDispatcherEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<ClientCredentialsProviderTelemetry>().As<IClientCredentialsProviderTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<CallbackEventTelemetry>().As<ICallbackEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<TelemetryService>().As<ITelemetryService>().InstancePerLifetimeScope();
        builder.RegisterType<OtelVariables>().SingleInstance();
        builder.RegisterType<PlatformMetrics>().SingleInstance();
        builder.RegisterType<SquadsSingleton>().SingleInstance();
        builder.RegisterType<DatadogMetricsService>().SingleInstance();
    }
}
