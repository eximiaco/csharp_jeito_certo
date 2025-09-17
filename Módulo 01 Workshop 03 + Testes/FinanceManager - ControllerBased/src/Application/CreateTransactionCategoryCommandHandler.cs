using FinanceManager.Domain;
using FinanceManager.Infra;
using MediatR;

namespace FinanceManager.Application;

public record CreateTransactionCategoryRequest(
    string Name,
    string Description,
    TransactionType CategoryType) : IRequest<CommandResult<CreateTransactionCategoryResponse>>;

public record CreateTransactionCategoryResponse(
    string Id,
    string Name,
    string Description,
    TransactionType CategoryType);

public class CreateTransactionCategoryCommandHandler(FinanceManagerDbContext context)
    : IRequestHandler<CreateTransactionCategoryRequest, CommandResult<CreateTransactionCategoryResponse>>
{
    public async Task<CommandResult<CreateTransactionCategoryResponse>> Handle(
        CreateTransactionCategoryRequest request, 
        CancellationToken cancellationToken)
    {
        var result = TransactionCategory.Create(
            request.Name,
            request.Description,
            request.CategoryType
        );

        if(result.TryGetValue(out TransactionCategory category))
        {
            return CommandResult<CreateTransactionCategoryResponse>.InvalidInput(result.Errors);
        }

        context.TransactionCategories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        return new CreateTransactionCategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.Type
        );
    }
}
