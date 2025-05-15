using GymErp.Common;

namespace GymErp.Tenant;

public class TenantAccessorRegisterMiddleware(
    TenantAccessor tenantAccessor,
    ITenantLocator tenantLocator,
    ITenantHttpAccessor tenantHttpAccessor)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Value == null || context.Request.Path.Value.Contains("swagger"))
            await next(context);
        else
        {
            var tenant = await tenantLocator.Get(tenantHttpAccessor.Get(), context.RequestAborted);
            tenantAccessor.Register(tenant);
            // Call the next delegate/middleware in the pipeline.
            await next(context);
            tenantAccessor.Dispose();    
        }
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantDbContext(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantAccessorRegisterMiddleware>();
    }
}