
using FinanceManager.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FinanceManager.Infra.HostedServices
{
    public class CreateAdminUserOptions
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class CreateAdminUserHostedService(
        IServiceProvider serviceProvider,
        IOptions<CreateAdminUserOptions> createAdminUserOptions) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
            var existingAdminAccount = await userManager.FindByEmailAsync(createAdminUserOptions.Value.Email);
            if (existingAdminAccount is not null)
            {
                return;
            }

            var adminAccount = new UserAccount(email: createAdminUserOptions.Value.Email, name: "Admin", role: "Admin");
            await userManager.CreateAsync(adminAccount, createAdminUserOptions.Value.Password);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
