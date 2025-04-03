using Microsoft.AspNetCore.Mvc;
using WorkflowCore.Interface;
using OrderService.Domain;
using OrderService.Infra;
using OrderService.Infra.Services;
using OrderService.Workflow;

namespace OrderService.Controllers;

public record CreateOrderRequest(string ProductName, IEnumerable<OrderItemRequest> Items);
public record OrderItemRequest(int Amount, decimal Price);

[ApiController]
[Route("orders")]
public class OrderController : ControllerBase
{
    [HttpPost("synchronous")]
    public async Task<object> CreateOrder(
        [FromBody] CreateOrderRequest orderRequest,
        [FromServices] PaymentService paymentService,
        [FromServices] ShippingService shippingService,
        [FromServices] StockService stockService,
        [FromServices] PlaygroundDbContext dbContext)
    {
        var ordeItems = orderRequest
                   .Items
                   .Select(item => new OrderItem(item.Amount, item.Price))
                   .ToList();

        var order = new Order(orderRequest.ProductName, ordeItems);

        try
        {
            var reservationId = await stockService.ReserveAsync(order);
            order.SetAsStockReserved(reservationId);

            var paymentId = await paymentService.MakePaymentAsync(order);
            order.SetAsPaymentApproved(paymentId);

            var shippingId = await shippingService.ShipAsync(order);
            order.SetAsShipped(shippingId);

            order.SetAsProcessed();

            dbContext.Add(order);
            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                OrderId = order.Id
            });
        }
        catch (Exception ex)
        {
            if (order.WasStockReserved())
            {
                await stockService.UndoStockReservationAsync(order);
            }

            if (order.WasPaymentApproved())
            {
                await paymentService.RefundPaymentAsync(order);
            }

            if (order.WasShipped())
            {
                await shippingService.CancelShippingAsync(order);
            }

            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("workflow")]
    public async Task<object> CreateOrderWorkflowAsync(
        [FromBody] CreateOrderRequest orderRequest,
        [FromServices] PlaygroundDbContext dbContext,
        [FromServices] IWorkflowHost workflowHost)
    {
        var ordeItems = orderRequest
            .Items
            .Select(item => new OrderItem(item.Amount, item.Price))
            .ToList();

        var order = new Order(orderRequest.ProductName, ordeItems);

        dbContext.Add(order);
        await dbContext.SaveChangesAsync();

        var workflowId = await workflowHost.StartWorkflow("OrderProcessingWorkflow", new OrderWorflowData
        {
            OrderId = order.Id
        });

        return Ok(new
        {
            OrderId = order.Id,
            WorkflowId = workflowId
        });
    }
}