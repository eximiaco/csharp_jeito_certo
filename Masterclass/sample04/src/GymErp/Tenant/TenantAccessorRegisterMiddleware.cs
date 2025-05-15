using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;

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