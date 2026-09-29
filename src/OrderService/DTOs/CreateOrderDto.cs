namespace OrderService.DTOs;

public class CreateOrderDto
{
    public Guid OriginPointId { get; set; }
    public Guid DestinationPointId { get; set; }
    public double Weight { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RecipientData { get; set; } = string.Empty;
}