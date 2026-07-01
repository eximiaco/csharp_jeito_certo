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
        await cacheProvider.SetAsync(
            $"{CacheKeyPrefix}:{category.Id}",
            category,
            slidingExpiration: TimeSpan.FromMinutes(5),
            absoluteExpiration: TimeSpan.FromHours(1));
    }
}
