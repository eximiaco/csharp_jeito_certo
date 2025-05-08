using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.TransactionCategories;

public record GetTransactionCategoryResponse(
    string Id,
    string Name,
    TransactionType CategoryType);

public class GetTransactionCategoriesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("transaction-categories", GetTransactionCategoriesAsync)
            .WithTags("transaction-category")
            .RequireAuthorization()
            .WithName("Get all transaction categories");
    }

    private static async Task<IResult> GetTransactionCategoriesAsync(
        FinanceManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var categories = await context.TransactionCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(categories.Select(c => new GetTransactionCategoryResponse(
            c.Id,
            c.Name,
            c.Type
        )));
    }
}
