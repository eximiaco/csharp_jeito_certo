using FinanceManager.Domain;
using FinanceManager.Infra;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace FinanceManager.Application;

public record CreateBankAccountRequest(string Name) : IRequest<CommandResult<CreateBankAccountReponse>>;

public record CreateBankAccountReponse(string Id, string Name);

public class CreateBankAccountCommandHandler(FinanceManagerDbContext context, IdentityUserService identityUserService)
    : IRequestHandler<CreateBankAccountRequest, CommandResult<CreateBankAccountReponse>>
{
    public async Task<CommandResult<CreateBankAccountReponse>> Handle(
        CreateBankAccountRequest command,
        CancellationToken cancellationToken)
    {
        var accountResult = BankAccount.Create(command.Name);

        if (accountResult.TryGetValue(out BankAccount account))
        {
            return CommandResult<CreateBankAccountReponse>.InvalidInput(accountResult.Errors);
        }

        var user = await identityUserService.GetCurrentUserAsync();

        user.AddBankAccount(account);

        await context.SaveChangesAsync(cancellationToken);

        return new CreateBankAccountReponse(account.Id, account.Name);
    }
}
