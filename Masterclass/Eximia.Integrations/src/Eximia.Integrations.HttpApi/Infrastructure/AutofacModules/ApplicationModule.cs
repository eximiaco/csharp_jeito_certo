using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Shared.Telemetry;
using Eximia.Integrations.IntegrationEvents.Telemetry;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Routing.Authentication;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.HttpApi.Infrastructure.AutofacModules;

public class ApplicationModule : Module
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
        builder.RegisterType<RegisterNewIntegrationEventOtelTelemetry>().As<IRegisterNewIntegrationEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<RouteIntegrationEventOtelTelemetry>().As<IRouteIntegrationEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<DispatcherEventTelemetry>().As<IDispatcherEventTelemetry>().InstancePerLifetimeScope();
        builder.RegisterType<ClientCredentialsProviderTelemetry>().As<IClientCredentialsProviderTelemetry>().InstancePerLifetimeScope();

        builder.RegisterType<TelemetryService>().As<ITelemetryService>().InstancePerLifetimeScope();
        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
        builder.RegisterType<OtelVariables>().SingleInstance();
        builder.RegisterType<TokenCache>().SingleInstance();
        builder.RegisterType<PlatformMetrics>().SingleInstance();
        builder.RegisterType<OutboxMetrics>().SingleInstance();
        builder.RegisterType<DatadogMetricsService>().SingleInstance();
        builder.RegisterType<SquadsSingleton>().SingleInstance();
    }
}
