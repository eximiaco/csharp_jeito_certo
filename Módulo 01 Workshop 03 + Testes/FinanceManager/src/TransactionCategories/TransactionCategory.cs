using FinanceManager.Crosscutting;

namespace FinanceManager.TransactionCategories;

public class TransactionCategory : Entity
{
    private TransactionCategory(string name, string description, TransactionType type)
    {
        Name = name;
        Description = description;
        Type = type;
    }

    public static Result<TransactionCategory> Create(string name, string description, TransactionType type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationErrors.TransactionCategory.NameIsRequired;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return ValidationErrors.TransactionCategory.DescriptionIsRequired;
        }

        return new TransactionCategory(name, description, type);
    }

    public string Name { get; init; }
    public string Description { get; init; }
    public TransactionType Type { get; init; }
}
