using Microsoft.EntityFrameworkCore;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using OrderService.Infra;
using OrderService.Infra.Services;

namespace OrderService.Workflow.Steps;

public class UndoProcessingStep(
    PlaygroundDbContext dbContext,
    ShippingService shippingService,
    StockService stockService,
    PaymentService paymentService,
    ILogger<UndoProcessingStep> logger) : StepBodyAsync
{
    public string OrderId { get; set; } = string.Empty;

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        var order = await dbContext
            .Orders
            .FirstAsync(o => o.Id == OrderId, context.CancellationToken);

        if (order.WasStockReserved())
        {
            logger.LogWarning("Undoing stock reservation for order {OrderId}", OrderId);

            await stockService.UndoStockReservationAsync(order);
        }

        if (order.WasPaymentApproved())
        {
            logger.LogWarning("Undoing payment for order {OrderId}", OrderId);

            await paymentService.RefundPaymentAsync(order);
        }

        if (order.WasShipped())
        {
            logger.LogWarning("Undoing shipping for order {OrderId}", OrderId);

            await shippingService.CancelShippingAsync(order);
        }

        order.Cancel();

        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogWarning("Order {OrderId} was canceled!", order.Id);

        return ExecutionResult.Next();
    }
}