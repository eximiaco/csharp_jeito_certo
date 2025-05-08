using System;
using FinanceManager.BankAccounts;
using FinanceManager.TransactionCategories;
using FinanceManager.UserAccounts;

namespace FinanceManager.UnitTests
{
    public static class Helpers
    {
        public static User CreateValidUser() =>
            User.Create("User for testes", "usertests@mail.com").Value!;

        public static BankAccount CreateValidBankAccount() =>
            BankAccount.Create("BankAccountForTests", CreateValidUser()).Value!;

        public static TransactionCategory CreateValidExpenseTransactionCategory() =>
            TransactionCategory.Create("Despesas", "Categoria para despesas", TransactionType.Expense).Value!;

        public static TransactionCategory CreateValidIncomeTransactionCategory() =>
            TransactionCategory.Create("Receitas", "Categoria para receitas", TransactionType.Income).Value!;
    }
}
