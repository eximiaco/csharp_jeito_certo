using FinanceManager.Crosscutting.Database;
using FinanceManager.UserAccounts;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceManager.Crosscutting;

public class IdentityUserService(FinanceManagerDbContext context, IHttpContextAccessor httpContextAccessor)
{
    public async Task<User> GetCurrentUserAsync()
    {
        var currentUserEmail = httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.Email)!;

        var user = await context
            .Users
            .FirstOrDefaultAsync(us => us.Email == currentUserEmail.Value);

        ArgumentNullException.ThrowIfNull(user);

        return user;
    }
}