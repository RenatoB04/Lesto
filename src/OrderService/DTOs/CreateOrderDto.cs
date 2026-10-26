namespace OrderService.DTOs;

public class CreateOrderDto
{
    // Origem e destino: um ponto do catálogo ou uma morada em texto livre (não os dois)
    public Guid? OriginPointId { get; set; }
    public Guid? DestinationPointId { get; set; }
    public string? OriginAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public double Weight { get; set; }
    public string Type { get; set; } = string.Empty;
    public string RecipientData { get; set; } = string.Empty;
}