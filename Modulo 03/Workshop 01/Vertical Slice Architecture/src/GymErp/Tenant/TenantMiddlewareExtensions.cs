namespace GymErp.Tenant;

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenant(
        this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<TenantAccessorRegisterMiddleware>()
            .UseMiddleware<TenantEfMiddleware>();
        
    }
}