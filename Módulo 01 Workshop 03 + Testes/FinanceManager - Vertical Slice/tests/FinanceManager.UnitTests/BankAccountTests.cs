using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;
using Shouldly;

namespace FinanceManager.UnitTests
{
    public class BankAccountTests
    {
        [Fact]
        public void Create_WithValidInput_ShouldReturnTransaction()
        {
            var user = User.Create("User", "user@email.com").Value;
            var accountResult = BankAccount.Create("Conta principal", user!);
            var account = accountResult.Value;

            accountResult.Success.ShouldBeTrue();
            account.ShouldNotBeNull();
            account!.Name.ShouldBe("Conta principal");
            account.User.ShouldBe(user);
            account.Balance.Amount.ShouldBe(0);
            account.Balance.Currency.ShouldBe("BRL");
            account.BudgetAlerts.ShouldBeEmpty();
            account.Transactions.ShouldBeEmpty();
        }

        [Fact]
        public void Create_WithInvalidName_ShouldNotCreateAndReturnErrors()
        {
            var user = User.Create("User", "user@email.com").Value;
            var accountResult = BankAccount.Create(string.Empty, user!);

            accountResult.Success.ShouldBeFalse();
            accountResult.Errors.ShouldContain(err => err == ValidationErrors.BankAccount.NameIsRequired);
        }

        [Fact]
        public void AddTransaction_WithExpenseCategory_ShouldDecreaseBalance()
        {
            var user = User.Create("User", "user@email.com").Value;
            var account = BankAccount.Create("Conta principal", user!).Value!;

            var transactionCategory = TransactionCategory.Create("Gastos", "Gastos do mês", TransactionType.Expense).Value;
            var transaction = Transaction.Create("Food", 150, 150, transactionCategory!, account).Value!;

            account.AddTransaction(transaction);
            account.Balance.Amount.ShouldBe(-150);
            account.Balance.Currency.ShouldBe("BRL");
        }

        [Fact]
        public void AddTransaction_WithIncomeCategory_ShouldIncreaseBalance()
        {
            var user = User.Create("User", "user@email.com").Value;
            var account = BankAccount.Create("Conta principal", user!).Value!;

            var transactionCategory = TransactionCategory.Create("Salário", "Salário", TransactionType.Income).Value;
            var transaction = Transaction.Create("Pagamento", 1500, 1500, transactionCategory!, account).Value!;

            account.AddTransaction(transaction);
            account.Balance.Amount.ShouldBe(1500);
            account.Balance.Currency.ShouldBe("BRL");
        }
    }
}
