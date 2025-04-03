using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using OrderService.Infra;
using OrderService.Infra.Services;

namespace OrderService.Workflow.Steps;

public class ProcessPaymentStep(
    ILogger<ProcessPaymentStep> logger,
    PlaygroundDbContext dbContext,
    PaymentService paymentService) : StepBodyAsync
{
    public string OrderId { get; set; } = string.Empty;

    public async override Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            logger.LogInformation("Processing payment for order {OrderId}", OrderId);

            var order = await dbContext
                .Orders
                .FirstAsync(or => or.Id == OrderId, context.CancellationToken);

            var paymentId = await paymentService.MakePaymentAsync(order);

            order.SetAsPaymentApproved(paymentId);

            await dbContext.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation("Payment processed for order {OrderId}", OrderId);

            return ExecutionResult.Next();
        }
        catch (Exception)
        {
            logger.LogError("An error ocurred processing payment for order {OrderId}", OrderId);
            
            var timeToSleep = TimeSpan.FromSeconds(5);

            return ExecutionResult.Sleep(timeToSleep, persistenceData: null);
        }
    }
}