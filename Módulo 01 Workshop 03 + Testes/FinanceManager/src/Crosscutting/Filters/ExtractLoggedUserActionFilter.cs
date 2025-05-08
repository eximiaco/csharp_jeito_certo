
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceManager.Crosscutting.Filters;

public class ExtractLoggedUserActionFilter(IdentityUserService identityUserService) : IActionFilter
{
    public async void OnActionExecuting(ActionExecutingContext context)
    {
        var currentUser = await identityUserService.GetCurrentUserAsync();
        context.HttpContext.Items.Add("CurrentUser", currentUser);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}