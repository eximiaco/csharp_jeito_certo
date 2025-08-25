using FinanceManager.Domain;
using FinanceManager.Infra;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Application;

public record CreateTransactionRequest(
    decimal Value,
    string Description,
    string CategoryId);

public record CreateTransactionCommand(
    CreateTransactionRequest Request,
    string AccountId) : IRequest<CommandResult<CreateTransactionResponse>>;

public record CreateTransactionResponse(
    string Id,
    string Description,
    decimal Value,
    string CategoryName);

public class CreateTransactionCommandHandler(
    FinanceManagerDbContext context,
    IMessageBroker brokerService,
    ExchangeService exchangeService,
    IdentityUserService identityUserService) : IRequestHandler<CreateTransactionCommand, CommandResult<CreateTransactionResponse>>
{
    public async Task<CommandResult<CreateTransactionResponse>> Handle(
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var currentUser = await identityUserService.GetCurrentUserAsync();

        var account = currentUser
            .BankAccounts
            .FirstOrDefault(ba => ba.Id == command.AccountId);

        if (account == null)
        {
            return new CommandResult<CreateTransactionResponse>(
                ValidationErrors.Account.AccountDoesNotExists, 
                CommandResultStatus.InvalidInput
            );
        }

        var category = await context.TransactionCategories.FirstOrDefaultAsync(tc =>
            tc.Id == command.Request.CategoryId, cancellationToken: cancellationToken);

        if (category == null)
        {
            return new CommandResult<CreateTransactionResponse>(ValidationErrors.Category.CategoryDoesNotExists, CommandResultStatus.InvalidInput);
        }

        var usdValueResult = await exchangeService.GetUsdValueAsync(command.Request.Value);
        if (!usdValueResult.Success)
        {
            return new CommandResult<CreateTransactionResponse>(usdValueResult.Errors, usdValueResult.Status);
        }

        var transactionResult = Transaction.Create(
            command.Request.Description,
            command.Request.Value,
            usdValueResult.Result,
            category,
            account
        );

        if(!transactionResult.TryGetValue(out Transaction transaction))
        {
            return new CommandResult<CreateTransactionResponse>(transactionResult.Errors, CommandResultStatus.InvalidInput);
        }

        account.AddTransaction(transaction);

        await context.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in account.Events)
        {
            await brokerService.ProduceMessageAsync(domainEvent);
        }

        return new CreateTransactionResponse(
            transaction.Id,
            transaction.Description,
            transaction.Value,
            category.Name
        );
    }
}
