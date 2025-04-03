using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using OrderService.Infra;
using OrderService.Infra.Services;

namespace OrderService.Workflow.Steps;

public class ScheduleShippingStep(
    PlaygroundDbContext dbContext,
    ShippingService shippingService,
    ILogger<ScheduleShippingStep> logger) : StepBodyAsync
{
    public string OrderId { get; set; } = string.Empty;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        logger.LogInformation("Scheduling shipping for order {OrderId}", OrderId);

        var order = await dbContext
            .Orders
            .FirstAsync(or => or.Id == OrderId);

        var shippingId = await shippingService.ShipAsync(order);

        order.SetAsShipped(shippingId);

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Shipping scheduled for order {OrderId}", OrderId);

        return ExecutionResult.Next();
    }
}
