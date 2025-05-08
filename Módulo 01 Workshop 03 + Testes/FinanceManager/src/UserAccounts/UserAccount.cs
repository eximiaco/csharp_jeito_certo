using FinanceManager.Crosscutting;
using Microsoft.AspNetCore.Identity;

namespace FinanceManager.UserAccounts
{
    public class UserAccountRole : IdentityRole
    {
        public UserAccountRole() { }
    }

    public class UserAccount : IdentityUser
    {
        public string Name { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiration { get; private set; }
        public string Role { get; private set; }

        public UserAccount(string email, string name, string role = "")
        {
            Email = email;
            Name = name;
            UserName = email;
            Role = role;
        }

        public static Result<UserAccount> Create(string email, string name, string role = "")
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationErrors.UserAccount.EmailIsRequired;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidationErrors.UserAccount.NameIsRequired;
            }

            return new UserAccount(email, name, role);
        }

        public void SetRefreshToken(string refreshToken, DateTime refreshTokenExpiration)
        {
            RefreshToken = refreshToken;
            RefreshTokenExpiration = refreshTokenExpiration;
        }
    }
}
