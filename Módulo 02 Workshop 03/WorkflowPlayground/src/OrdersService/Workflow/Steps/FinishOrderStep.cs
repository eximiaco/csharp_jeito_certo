using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using OrderService.Infra;

namespace OrderService.Workflow.Steps;

public class FinishOrderStep(PlaygroundDbContext dbContext, ILogger<FinishOrderStep> logger) : StepBodyAsync
{
    public string OrderId { get; set; } = string.Empty;
    
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var order = await dbContext
            .Orders
            .FirstAsync(or => or.Id == OrderId);

        order.SetAsProcessed();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Order {OrderId} finished", OrderId);

        return ExecutionResult.Next();
    }
}