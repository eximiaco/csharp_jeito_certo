using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.Endpoints;

public record struct GetBankAccountResponse(string Id, string Name, decimal Balance, IEnumerable<TransactionResponse> Transactions)
{
    public static implicit operator GetBankAccountResponse(BankAccount account) =>
        new(account.Id, account.Name, account.Balance, account.Transactions.Select(t => (TransactionResponse)t));
}

public record struct TransactionResponse(string Id, string Description, decimal Value, string Type)
{
    public static implicit operator TransactionResponse(Transaction transaction) =>
        new(transaction.Id, transaction.Description, transaction.Value, transaction.Type.ToString());
}

public class GetBankAccountsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("bank-accounts", GetBankAccountsAsync)
            .WithTags("bank-account")
            .RequireAuthorization()
            .WithName("Get created bank accounts");
    }

    private async static Task<IResult> GetBankAccountsAsync(
       ILogger<GetBankAccountsEndpoint> logger,
       FinanceManagerDbContext context,
       [FromQuery] int skip = 0, 
       [FromQuery] int take = 10,
       CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all bank accounts");

        var accounts = await context
            .BankAccounts
            .Include(ac => ac.Transactions)
            .Include(ac => ac.BudgetAlerts)
            .Skip(skip)
            .Take(take)
            .Select(bc => (GetBankAccountResponse)bc)
            .ToListAsync(cancellationToken: cancellationToken);

        return Ok(accounts);
    }
}
