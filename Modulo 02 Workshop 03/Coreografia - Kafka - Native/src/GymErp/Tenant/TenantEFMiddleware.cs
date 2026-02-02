using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;

namespace GymErp.Tenant;

public class TenantEfMiddleware(
    IEfDbContextFactory<SubscriptionsDbContext> factory,
    IEfDbContextAccessor<SubscriptionsDbContext> accessor,
    TenantAccessor tenantAccessor)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await using var contexto = await factory.CriarAsync(tenantAccessor.Tenant.Name);
        accessor.Register(contexto);
        // Call the next delegate/middleware in the pipeline.
        await next(context);
        accessor.Clear();
    }
}