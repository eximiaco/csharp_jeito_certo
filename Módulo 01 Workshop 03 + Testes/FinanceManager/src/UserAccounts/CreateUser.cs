using System;
using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.AspNetCore.Identity;

namespace FinanceManager.UserAccounts;

public record struct CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string ConfirmPassword);

public record struct CreateUserResponse(
    string Id,
    string Email);

public class CreateUserEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("user-account", CreateUserAsync)
            .WithTags("user")
            .WithName("Create a new user");
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        FinanceManagerDbContext context,
        UserManager<UserAccount> userManager,
        CancellationToken cancellationToken)
    {
        var userResult = User.Create(request.Name, request.Email);
        if (!userResult.TryGetValue(out User user))
        {
            return BadRequest(userResult.Errors);
        }

        var userAccountResult = UserAccount.Create(request.Email, request.Name);

        if(!userAccountResult.TryGetValue(out UserAccount userAccount))
        {
            return BadRequest(userAccountResult.Errors);
        }

        var result = await userManager.CreateAsync(userAccount, request.Password);

        if (!result.Succeeded)
        {
            var identityResults = result
                .Errors
                .Select(err => new ErrorMessage(err.Code, err.Description));

            return BadRequest(identityResults);
        }

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new CreateUserResponse(user.Id, user.Email));
    }
}
