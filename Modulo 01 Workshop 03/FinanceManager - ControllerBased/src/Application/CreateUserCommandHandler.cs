using FinanceManager.Domain;
using FinanceManager.Infra;
using FinanceManager.Infra.Auth;
using Microsoft.AspNetCore.Identity;

namespace FinanceManager.Application;

public record struct CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword);

public record struct CreateUserResponse(
    string Id,
    string Email);

public class CreateUserCommandHandler(
    FinanceManagerDbContext context,
    UserManager<UserAccount> userManager)
{
    public async Task<CommandResult<CreateUserResponse>> Handle(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userResult = User.Create(request.Name, request.Email);
        if (!userResult.TryGetValue(out User user))
        {
            return new CommandResult<CreateUserResponse>(userResult.Errors, CommandResultStatus.InvalidInput);
        }

        var userAccountResult = UserAccount.Create(request.Email, request.Name);

        if(!userAccountResult.TryGetValue(out UserAccount userAccount))
        {
            return new CommandResult<CreateUserResponse>(userAccountResult.Errors, CommandResultStatus.InvalidInput);
        }

        var result = await userManager.CreateAsync(userAccount, request.Password);

        if (!result.Succeeded)
        {
            var identityResults = result
                .Errors
                .Select(err => new ErrorMessage(err.Code, err.Description));

            return new CommandResult<CreateUserResponse>(identityResults, CommandResultStatus.InvalidInput);
        }

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(user.Id, user.Email);
    }
}
