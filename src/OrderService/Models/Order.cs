namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? CourierId { get; set; }

    public Guid OriginPointId { get; set; }
    public Point Origin { get; set; } = null!;

    public Guid DestinationPointId { get; set; }
    public Point Destination { get; set; } = null!;

    public double Weight { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RecipientData { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? Reason { get; set; }

    public double Distance { get; set; }
    public double Duration { get; set; }
}