using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.UserAccounts;

public record struct LoginRequest(
    string Email,
    string Password);

public record struct LoginResponse(
    string AccessToken, 
    string RefreshToken);

public class LoginEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("user-account/login", LoginAsync)
            .WithTags("user")
            .WithName("Login a user");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<UserAccount> userManager,
        SignInManager<UserAccount> signInManager,
        ITokenProviderService tokenProviderService)
    {
        var signInResult = await signInManager.PasswordSignInAsync(request.Email, request.Password, false, true);
        if (!signInResult.Succeeded)
        {
            return Unauthorized();
        }

        var user = (await userManager.FindByNameAsync(request.Email))!;

        var accessToken = tokenProviderService.GenerateToken(user);
        var refreshToken = tokenProviderService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(15));
        await userManager.UpdateAsync(user);

        return Ok(new LoginResponse(accessToken, refreshToken));
    }
}   

