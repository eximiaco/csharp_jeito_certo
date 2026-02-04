using FinanceManager.Crosscutting;
using FinanceManager.Crosscutting.Database;

namespace FinanceManager.TransactionCategories;

public record CreateTransactionCategoryRequest(
    string Name,
    string Description,
    TransactionType CategoryType);

public record CreateTransactionCategoryResponse(
    string Id,
    string Name,
    string Description,
    TransactionType CategoryType);

public class CreateTransactionCategoryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("transaction-categories", CreateTransactionCategoryAsync)
            .WithTags("transaction-category")
            .RequireAuthorization()
            .WithName("Create a new transaction category");
    }

    private async static Task<IResult> CreateTransactionCategoryAsync(
        CreateTransactionCategoryRequest request,
        FinanceManagerDbContext context,
        CancellationToken cancellationToken)
    {
        var result = TransactionCategory.Create(
            request.Name,
            request.Description,
            request.CategoryType
        );

        if(!result.TryGetValue(out TransactionCategory category))
        {
            return BadRequest(result.Errors);
        }

        context.TransactionCategories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        return Ok(new CreateTransactionCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.Type
        ));
    }
}
