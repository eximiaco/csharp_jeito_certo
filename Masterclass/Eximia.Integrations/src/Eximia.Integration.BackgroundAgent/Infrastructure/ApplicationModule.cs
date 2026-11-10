using Autofac;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

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
        builder.RegisterType<PostgresConnectionStringFactory>().SingleInstance();
    }
}
