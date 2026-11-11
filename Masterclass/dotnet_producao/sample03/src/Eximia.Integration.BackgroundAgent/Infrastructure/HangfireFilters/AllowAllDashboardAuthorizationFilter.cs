using Hangfire.Dashboard;

namespace Eximia.Integration.BackgroundAgent.Infrastructure.HangfireFilters;

public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true; // Permite qualquer requisição
    }
}