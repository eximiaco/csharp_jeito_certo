using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using FinanceManager.Crosscutting.Filters;

namespace FinanceManager.BankAccounts.Endpoints;

public record struct CreateBankAccountRequest(string Name);
public record struct CreateBankAccountReponse(string Id, string Name);

public class CreateBankAccountEndpoint : IEndpoint
{
    private const string Route = "bank-accounts";

    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, CreateBankAccountAsync)
            .WithTags("bank-account")
            .RequireAuthorization()
            .WithName("Create a new bank account");
    }

    private async static Task<IResult> CreateBankAccountAsync(
       IdentityUserService identityUserService,
       CreateBankAccountRequest command,
       FinanceManagerDbContext context,
       CancellationToken cancellationToken)
    {
        var currentUser = await identityUserService.GetCurrentUserAsync();
        var accountResult = BankAccount.Create(command.Name, currentUser);

        if (!accountResult.TryGetValue(out BankAccount account))
        {
            return BadRequest(accountResult.Errors);
        }

        var user = await identityUserService.GetCurrentUserAsync();

        user.AddBankAccount(account);

        await context.SaveChangesAsync(cancellationToken);

        return Created($"{Route}/{account.Id}", new CreateBankAccountReponse(account.Id, account.Name));
    }
}
