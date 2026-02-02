using FinanceManager.Domain;
using FinanceManager.Infra;

namespace FinanceManager.Application;

public record CreateTransactionCategoryRequest(
    string Name,
    string Description,
    TransactionType CategoryType);

public record CreateTransactionCategoryResponse(
    string Id,
    string Name,
    string Description,
    TransactionType CategoryType);

public class CreateTransactionCategoryCommandHandler(
    FinanceManagerDbContext context,
    CacheProvider cacheProvider)
{
    private const string CacheKeyPrefix = "transaction-categories";
    private const string CacheKeyAll = $"{CacheKeyPrefix}:all";

    public async Task<CommandResult<CreateTransactionCategoryResponse>> Handle(
        CreateTransactionCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var result = TransactionCategory.Create(
            request.Name,
            request.Description,
            request.CategoryType
        );

        if(!result.TryGetValue(out TransactionCategory category))
        {
            return CommandResult<CreateTransactionCategoryResponse>.InvalidInput(result.Errors);
        }

        context.TransactionCategories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        await UpdateCacheAfterCreateAsync(category);

        return new CreateTransactionCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.Type
        );
    }

    private async Task UpdateCacheAfterCreateAsync(TransactionCategory category)
    {
        await cacheProvider.RemoveAsync(CacheKeyAll);
        await cacheProvider.RemoveAsync($"{CacheKeyPrefix}:type:{category.Type}");
        
        await cacheProvider.SetAsync(
            $"{CacheKeyPrefix}:{category.Id}",
            category,
            slidingExpiration: TimeSpan.FromMinutes(5),
            absoluteExpiration: TimeSpan.FromHours(1));
    }
}
