using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.Endpoints;

public class GetSingleBankAccountEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("bank-accounts/{id}", GetSingleBankAccountAsync)
            .WithTags("bank-account")
            .RequireAuthorization()
            .WithName("Get a single bank account");
    }

    private async static Task<IResult> GetSingleBankAccountAsync(
        [FromRoute] string id,
        ILogger<GetSingleBankAccountEndpoint> logger,
        FinanceManagerDbContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting single bank account");

        var account = context
            .BankAccounts
            .AsNoTracking()
            .Include(b => b.Transactions)
            .FirstOrDefault(bc => bc.Id == id);

        if (account is null)
        {
            return NoContent();
        }

        return account is null 
            ? NoContent()
            : Ok((GetBankAccountResponse)account);
    }
}