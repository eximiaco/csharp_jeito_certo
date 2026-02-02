using FinanceManager.Domain;
using FinanceManager.Infra;
using FinanceManager.Infra.Auth;
using Microsoft.AspNetCore.Identity;

namespace FinanceManager.Application;

public record struct LoginRequest(
    string Email,
    string Password);

public record struct LoginResponse(
    string AccessToken, 
    string RefreshToken);

public class LoginCommandHandler(
    UserManager<UserAccount> userManager,
    SignInManager<UserAccount> signInManager,
    ITokenProviderService tokenProviderService)
{
    public async Task<CommandResult<LoginResponse>> Handle(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var signInResult = await signInManager.PasswordSignInAsync(request.Email, request.Password, false, true);
        if (!signInResult.Succeeded)
        {
            return new CommandResult<LoginResponse>(CommandResultStatus.Unauthorized);
        }

        var user = (await userManager.FindByNameAsync(request.Email))!;

        var accessToken = tokenProviderService.GenerateToken(user);
        var refreshToken = tokenProviderService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(15));
        await userManager.UpdateAsync(user);

        return new LoginResponse(accessToken, refreshToken);
    }
}
