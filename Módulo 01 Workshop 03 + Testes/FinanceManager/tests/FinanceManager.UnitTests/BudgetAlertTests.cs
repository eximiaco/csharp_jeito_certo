using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;
using NSubstitute;
using Shouldly;

namespace FinanceManager.UnitTests
{
    public class BudgetAlertTests
    {
        [Fact]
        public void Create_WithValidInput_ShouldCreate()
        {
            var user = User.Create("User", "user@email.com").Value;
            var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
            var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

            var budgetAlertResult = BudgetAlert.Create(bankAccount, category, 100);

            budgetAlertResult.Success.ShouldBeTrue();
        }

        [Fact]
        public void Create_WithCategoryTypeAsIncone_ShouldNotCreateAndReturnError()
        {
            var user = User.Create("User", "user@email.com").Value;
            var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
            var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Income).Value!;

            var budgetAlertResult = BudgetAlert.Create(bankAccount, category, 100);

            budgetAlertResult.Success.ShouldBeFalse();
            budgetAlertResult.Errors.ShouldNotBeEmpty();
            budgetAlertResult.Errors.ShouldContain(err => err == ValidationErrors.BudgetAlert.CategoryShouldBeExpense);
        }

        [Fact]
        public void Create_WithInvalidAmount_ShouldNotCreateAndReturnError()
        {
            var user = User.Create("User", "user@email.com").Value;
            var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
            var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

            var budgetAlertResult = BudgetAlert.Create(bankAccount, category, -10);

            budgetAlertResult.Success.ShouldBeFalse();
            budgetAlertResult.Errors.ShouldNotBeEmpty();
            budgetAlertResult.Errors.ShouldContain(err => err == ValidationErrors.Money.AmountShouldBeGreaterThanZero);
        }

        [Fact]
        public void Create_WithNullBankAccount_ShouldNotCreateAndThrowException()
        {
            var user = User.Create("User", "user@email.com").Value;
            var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

            Should.Throw<ArgumentNullException>(() => BudgetAlert.Create(null!, category, 100));
        }

        [Fact]
        public void Create_WithNullTransactionCategory_ShouldNotCreateAndThrowException()
        {
            var user = User.Create("User", "user@email.com").Value;
            var bankAccount = BankAccount.Create("BankAccount", user!).Value!;

            Should.Throw<ArgumentNullException>(() => BudgetAlert.Create(bankAccount, null!, 100));
        }

        [Fact]
        public void HasExceedMaxAllowed_WithBankAccountAndTransactions_ShouldReturnTrueIFExceed()
        {
            var bankAccount = Helpers.CreateValidBankAccount();
            var category = Helpers.CreateValidExpenseTransactionCategory();
            var transactionA = Transaction.Create("Transaction A", 150, 100, category, bankAccount).Value!;
            var transactionB = Transaction.Create("Transaction B", 150, 100, category, bankAccount).Value!;

            bankAccount.AddTransaction(transactionA);
            bankAccount.AddTransaction(transactionB);

            var budgetAlert = BudgetAlert.Create(bankAccount, category, maxAllowed: 200).Value!;
            budgetAlert.HasExceedMaxAllowed(bankAccount).ShouldBeTrue();
        }
    }
}