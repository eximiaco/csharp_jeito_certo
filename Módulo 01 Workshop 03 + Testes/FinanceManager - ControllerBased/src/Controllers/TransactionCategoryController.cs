using Asp.Versioning;
using FinanceManager.Application;
using FinanceManager.Domain;
using FinanceManager.Infra;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{v:apiVersion}/transaction-categories")]
public class TransactionCategoryController : ControllerBase
{
    private const string CacheKeyPrefix = "transaction-categories";
    private const string CacheKeyAll = $"{CacheKeyPrefix}:all";
    
    private static string GetCacheKeyById(string id) => $"{CacheKeyPrefix}:{id}";

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateTransactionCategory(
        [FromServices] IMediator mediator,
        [FromBody] CreateTransactionCategoryRequest transactionRequest)
    {
        var result = await mediator.Send(transactionRequest);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetTransactionCategories(
        [FromServices] FinanceManagerDbContext context,
        [FromServices] CacheProvider cacheProvider)
    {
        var categories = await cacheProvider.GetOrCreateAsync(
            CacheKeyAll,
            async () =>
            {
                var result = await context.TransactionCategories
                    .AsNoTracking()
                    .ToListAsync();
                
                return result ?? new List<TransactionCategory>();
            },
            slidingExpiration: TimeSpan.FromMinutes(5),
            absoluteExpiration: TimeSpan.FromHours(1));

        return Ok(categories);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransactionCategoryById(
        [FromRoute] string id,
        [FromServices] FinanceManagerDbContext context,
        [FromServices] CacheProvider cacheProvider)
    {
        var cacheKey = GetCacheKeyById(id);

        var category = await cacheProvider.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var found = await context.TransactionCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);

                return found;
            },
            slidingExpiration: TimeSpan.FromMinutes(5),
            absoluteExpiration: TimeSpan.FromHours(1));

        if (category == null)
        {
            return NotFound(new { message = $"Transaction category with id '{id}' not found." });
        }

        return Ok(category);
    }

    [Authorize]
    [HttpGet("by-type/{type}")]
    public async Task<IActionResult> GetTransactionCategoriesByType(
        [FromRoute] TransactionType type,
        [FromServices] FinanceManagerDbContext context,
        [FromServices] CacheProvider cacheProvider)
    {
        var cacheKey = $"{CacheKeyPrefix}:type:{type}";

        var categories = await cacheProvider.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var result = await context.TransactionCategories
                    .AsNoTracking()
                    .Where(c => c.Type == type)
                    .ToListAsync();
                
                return result ?? new List<TransactionCategory>();
            },
            slidingExpiration: TimeSpan.FromMinutes(5),
            absoluteExpiration: TimeSpan.FromHours(1));

        return Ok(categories);
    }
}
