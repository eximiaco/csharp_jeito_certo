using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Eximia.Integration.BackgroundAgent.Infrastructure.HangfireFilters;

public class AzureADAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true;
    }
}
