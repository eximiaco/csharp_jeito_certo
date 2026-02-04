namespace FinanceManager.Crosscutting
{
    public static class ValidationErrors
    {
        public static class Account
        {
            public static readonly ErrorMessage AccountDoesNotExists = new("ACCOUNT_DOES_NOT_EXISTS", "A conta informada não existe");
        }

        public static class Category
        {
            public static readonly ErrorMessage CategoryDoesNotExists = new("CATEGORY_DOES_NOT_EXISTS", "A categoria informada não existe");
        }

        public static class General
        {
            public static ErrorMessage UnknownError(string message) => new("UNKNOWN_ERROR", message);
        }

        public static class BudgetAlert
        {
            public static ErrorMessage ValueShouldBeGreaterThanZero() => new("VALUE_SHOULD_BE_GREATER_THAN_ZERO", "O valor deve ser maior que zero");
            public static ErrorMessage ThresholdShouldBeGreaterThanZero() => new("THRESHOLD_SHOULD_BE_GREATER_THAN_ZERO", "O limite deve ser maior que zero");
            public static ErrorMessage CategoryShouldBeExpense => new("CATEGORY_SHOULD_BE_EXPENSE", "A categoria deve ser do tipo despesa");
        }

        public static class BankAccount
        {
            public static readonly ErrorMessage NameIsRequired = new("BANK_ACCOUNT_NAME_IS_REQUIRED", "O nome da conta é obrigatório");
        }

        public static class Transaction
        {
            public static readonly ErrorMessage DescriptionIsRequired = new("DESCRIPTION_IS_REQUIRED", "A descrição da transação é obrigatória");
            public static readonly ErrorMessage ValueShouldBeGreaterThanZero = new("VALUE_SHOULD_BE_GREATER_THAN_ZERO", "O valor da transação deve ser maior que zero");
            public static readonly ErrorMessage UsdValueShouldBeGreaterThanZero = new("USD_VALUE_SHOULD_BE_GREATER_THAN_ZERO", "O valor em dólar da transação deve ser maior que zero");
        }

        public static class TransactionCategory
        {
            public static readonly ErrorMessage NameIsRequired = new("CATEGORY_NAME_IS_REQUIRED", "O nome da categoria é obrigatório");
            public static readonly ErrorMessage DescriptionIsRequired = new("DESCRIPTION_IS_REQUIRED", "A descrição da categoria é obrigatória");
        }

        public static class User
        {
            public static readonly ErrorMessage NameIsRequired = new("NAME_IS_REQUIRED", "O nome do usuário é obrigatório");
            public static readonly ErrorMessage EmailIsRequired = new("EMAIL_IS_REQUIRED", "O e-mail do usuário é obrigatório");
        }

        public static class UserAccount
        {
            public static readonly ErrorMessage EmailIsRequired = new("EMAIL_IS_REQUIRED", "O e-mail do usuário é obrigatório");
            public static readonly ErrorMessage NameIsRequired = new("NAME_IS_REQUIRED", "O nome do usuário é obrigatório");
        }

        public static class Money
        {
            public static readonly ErrorMessage AmountShouldBeGreaterThanZero = new("AMOUNT_SHOULD_BE_GREATER_THAN_ZERO", "O valor deve ser maior que zero");
            public static readonly ErrorMessage CurrencyShouldNotBeEmpty = new("CURRENCY_SHOULD_NOT_BE_EMPTY", "A moeda deve ser informada");
        }

        public static class Percentage
        {
            public static readonly ErrorMessage InvalidValue = new("INVALID_VALUE", "O valor deve ser entre 0 e 100");
        }
    }
}
