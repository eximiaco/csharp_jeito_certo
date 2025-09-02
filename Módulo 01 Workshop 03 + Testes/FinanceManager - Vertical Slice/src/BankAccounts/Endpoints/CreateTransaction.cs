using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.BankAccounts.Endpoints;

public record struct CreateTransactionRequest(
    decimal Value,
    string Description,
    string CategoryId);

public record struct CreateTransactionResponse(
    string Id,
    string Description,
    decimal Value,
    string CategoryName);

public class CreateTransactionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("bank-accounts/{accountId}/transactions", CreateTransactionAsync)
            .WithTags("transaction")
            .RequireAuthorization()
            .WithName("Create a new transaction");
    }

    private async static Task<IResult> CreateTransactionAsync(
        [FromRoute] string accountId,
        CreateTransactionRequest request,
        FinanceManagerDbContext context,
        EventDispatcherService eventDispatcherService,
        ExchangeService exchangeService,
        ILogger<CreateTransactionEndpoint> logger,
        CancellationToken cancellationToken)
    {
        var account = await context
            .BankAccounts
            .Include(b => b.Transactions)
            .Include(b => b.BudgetAlerts)
            .FirstOrDefaultAsync(b => b.Id == accountId, cancellationToken);

        if (account == null)
        {
            return BadRequest(ValidationErrors.Account.AccountDoesNotExists);
        }

        var category = await context.GetCategoryAsync(request.CategoryId, cancellationToken: cancellationToken);
        if (category == null)
        {
            return BadRequest(ValidationErrors.Category.CategoryDoesNotExists);
        }

        var usdValueResult = await exchangeService.GetUsdValueAsync(request.Value);
        if (!usdValueResult.TryGetValue(out decimal usdValue))
        {
            return BadRequest(usdValueResult.Errors);
        }

        var transactionResult = Transaction.Create(
            request.Description,
            request.Value,
            usdValue,
            category,
            account
        );

        if (!transactionResult.TryGetValue(out Transaction transaction))
        {
            return BadRequest(transactionResult.Errors);
        }

        account.AddTransaction(transaction);

        await context.SaveChangesAsync(cancellationToken);

        await eventDispatcherService.DispatchEventsAsync(account.Events);

        logger.LogInformation(
            "Transaction for account {AccountName} with value {TransactionValue} created", 
            account.Name, 
            transaction.Value
        );

        return Ok(new CreateTransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Value,
            category.Name
        ));
    }
}