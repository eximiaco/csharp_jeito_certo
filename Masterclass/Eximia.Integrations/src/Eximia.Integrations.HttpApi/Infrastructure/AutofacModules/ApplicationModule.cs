using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

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

        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
    }
}
