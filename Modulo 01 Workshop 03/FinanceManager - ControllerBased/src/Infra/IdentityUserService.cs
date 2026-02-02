using FinanceManager.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinanceManager.Infra;

public class IdentityUserService(FinanceManagerDbContext context, IHttpContextAccessor httpContextAccessor)
{
    public async Task<User> GetCurrentUserAsync()
    {
        var currentUserEmail = httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.Email)!;

        var user = await context
            .Users
            .Include(us => us.BankAccounts)
                .ThenInclude(ba => ba.Transactions)
            .Include(us => us.BankAccounts)
                .ThenInclude(ba => ba.BudgetAlerts)
            .FirstAsync(us => us.Email == currentUserEmail.Value);

        return user;
    }
}
