namespace OrderService.Data;

public class Order
{
    public Order()
    {
        Status = "In process";
        Id = Guid.NewGuid().ToString();
    }

    public string Id { get; init; }
    public string Status { get; private set; }

    internal void SetAsFinished()
    {
        Status = "Finished";
    }

    internal void SetAsCanceled()
    {
        Status = "Canceled";
    }
}
