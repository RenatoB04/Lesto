namespace OrderService.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RecipientData { get; set; } = string.Empty;
    public double Distance { get; set; }
    public double Duration { get; set; }
    public PointDto Origin { get; set; } = null!;
    public PointDto Destination { get; set; } = null!;
}