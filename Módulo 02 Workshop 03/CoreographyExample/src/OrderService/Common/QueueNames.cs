
namespace OrderService.Common;

public static class QueueNames
{
    public const string OrderCreatedQueue = "ecommerce.order.created";
    public const string StockReservedQueue = "ecommerce.stock.reserved";
    public const string PaymentProcessedQueue = "ecommerce.payment.processed";
    public const string ShippingScheduledQueue = "ecommerce.shipping.scheduled";
    public const string UndoOrderCreationQueue = "ecommerce.order.undo-creation";
    public const string UndoReserveStockQueue = "ecommerce.stock.undo-reservation";
    public const string UndoPaymentProcessingQueue = "ecommerce.payment.undo-processing";
    public const string UndoShippingSchedulingQueue = "ecommerce.shipping.undo-scheduling";
}

public record OrderCreatedEvent(string OrderId);
public record UndoOrderCreationEvent(string OrderId);

public record StockReservedEvent(string OrderId);
public record UndoStockReservationEvent(string OrderId);

public record PaymentProcessedEvent(string OrderId);
public record UndoPaymentProcessingEvent(string OrderId);

public record ShippingScheduledEvent(string OrderId);
public record UndoShippingSchedulingEvent(string OrderId);