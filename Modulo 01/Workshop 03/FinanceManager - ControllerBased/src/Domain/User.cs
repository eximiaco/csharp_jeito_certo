using FinanceManager.Infra;

namespace FinanceManager.Domain
{
    public class User : Entity
    {
        private readonly List<BankAccount> _bankAccounts;

        private User(string name, string email)
        {
            _bankAccounts = [];
            Email = email;
            Name = name;
        }

        public static Result<User> Create(string name, string email)
        {
            var validationErrors = new List<ErrorMessage>();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                validationErrors.Add(ValidationErrors.User.NameIsRequired);
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                validationErrors.Add(ValidationErrors.User.EmailIsRequired);
            }

            return validationErrors.Count != 0
                ? Result<User>.Fail(validationErrors)
                : new User(name, email);
        }

        public string Email { get; private set; }
        public string Name { get; private set; }
        public IReadOnlyCollection<BankAccount> BankAccounts => _bankAccounts;

        public void AddBankAccount(BankAccount bankAccount)
        {
            _bankAccounts.Add(bankAccount);
        }
    }
}
