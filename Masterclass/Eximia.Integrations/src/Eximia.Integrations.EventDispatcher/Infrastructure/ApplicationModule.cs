using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Routing.Authentication;

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
        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
        builder.RegisterType<TokenCache>().SingleInstance();
    }
}
