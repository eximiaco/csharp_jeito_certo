using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using OrderService.Infra;
using OrderService.Infra.Services;

namespace OrderService.Workflow.Steps;

public class ReserveStockStep(
    PlaygroundDbContext dbContext,
    StockService stockService,
    ILogger<ReserveStockStep> logger) : StepBodyAsync
{
    public string OrderId { get; set; } = string.Empty;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        logger.LogInformation("Reserving stock for order {OrderId}", OrderId);

        var order = await dbContext
            .Orders
            .FirstAsync(or => or.Id == OrderId, context.CancellationToken);

        var reservationId = await stockService.ReserveAsync(order);

        order.SetAsStockReserved(reservationId);

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stock reserved for order {OrderId}", OrderId);

        return ExecutionResult.Next();
    }
}