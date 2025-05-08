using FinanceManager.BankAccounts;
using FinanceManager.Crosscutting;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;
using Shouldly;

namespace FinanceManager.UnitTests;

public class TransactionTests
{
    [Fact]
    public void Create_WithValidInput_ShouldCreate()
    {
        var user = User.Create("User", "user@email.com").Value;
        var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
        var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

        var transaction = Transaction.Create("Expenses", 150, 150, category, bankAccount).Value!;

        transaction.ShouldNotBeNull();
        transaction.CategoryId.ShouldBe(category.Id);
        transaction.AccountId.ShouldBe(bankAccount.Id);
        transaction.UsdValue.Amount.ShouldBe(150);
        transaction.UsdValue.Currency.ShouldBe("USD");
        transaction.Value.Amount.ShouldBe(150);
        transaction.Value.Currency.ShouldBe("BRL");
    }

    [Fact]
    public void Create_WithNullUsdValue_ShouldCreateAndAssumeDefaultValue()
    {
        var user = User.Create("User", "user@email.com").Value;
        var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
        var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

        var transaction = Transaction.Create("Expenses", 150, null, category, bankAccount).Value!;

        transaction.ShouldNotBeNull();
        transaction.UsdValue.Amount.ShouldBe(0);
        transaction.UsdValue.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_WithInvalidAmount_ShouldNotCreateAndReturnErrors()
    {
        var user = User.Create("User", "user@email.com").Value;
        var bankAccount = BankAccount.Create("BankAccount", user!).Value!;
        var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

        var transactionResult = Transaction.Create("Expenses", -100, 150, category, bankAccount);

        transactionResult.Success.ShouldBeFalse();
        transactionResult.Errors.ShouldNotBeEmpty();
        transactionResult.Errors.ShouldContain(err => err == ValidationErrors.Money.AmountShouldBeGreaterThanZero);
    }

    [Fact]
    public void Create_WithNullCategory_ShouldThrowArgumentNullException()
    {
        var user = User.Create("User", "user@email.com").Value;
        var bankAccount = BankAccount.Create("BankAccount", user!).Value!;

        Should.Throw<ArgumentNullException>(() => Transaction.Create("Expenses", 150, 150, null!, bankAccount));
    }

    [Fact]
    public void Create_WithNullAccount_ShouldThrowArgumentNullException()
    {
        var user = User.Create("User", "user@email.com").Value;
        var category = TransactionCategory.Create("Category", "Category transaction", TransactionType.Expense).Value!;

        Should.Throw<ArgumentNullException>(() => Transaction.Create("Expenses", 150, 150, category, null!));
    }
}