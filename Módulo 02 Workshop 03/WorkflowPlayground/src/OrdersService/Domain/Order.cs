

namespace OrderService.Domain;

public enum OrderStatus
{
    Pending,
    Processed,
    Canceled
}

public class Order
{
    public string Id { get; private set; }
    public string ProductName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string? StockReservationId { get; private set; }
    public string? PaymentId { get; private set; }
    public string? ShippingId { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Order() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public Order(string productName, IReadOnlyCollection<OrderItem> items)
    {
        Id = Guid.NewGuid().ToString();
        ProductName = productName;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Items = items;
        Status = OrderStatus.Pending;
    }

    public bool WasStockReserved() => !string.IsNullOrWhiteSpace(StockReservationId);

    public bool WasPaymentApproved() => !string.IsNullOrWhiteSpace(PaymentId);

    public bool WasShipped() => !string.IsNullOrWhiteSpace(ShippingId);

    public void SetAsPaymentApproved(string paymentId)
    {
        PaymentId = paymentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsStockReserved(string stockReservationId)
    {
        StockReservationId = stockReservationId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsShipped(string shippingId)
    {
        ShippingId = shippingId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsProcessed()
    {
        Status = OrderStatus.Processed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        StockReservationId = null;
        ShippingId = null;
        PaymentId = null;
        
        Status = OrderStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class OrderItem
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private OrderItem() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public OrderItem(int quantity, decimal price)
    {
        Quantity = quantity;
        Price = price;
        Id = Guid.NewGuid().ToString();
    }

    public string Id { get; private set; }

    public int Quantity { get; }
    public decimal Price { get; }
}