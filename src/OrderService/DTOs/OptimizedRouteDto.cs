namespace OrderService.DTOs;

public class OptimizedRouteDto
{
    // "osrm" (distâncias reais por estrada) ou "estimativa" (linha reta corrigida, se o OSRM falhar)
    public string Source { get; set; } = string.Empty;
    public List<RouteStopDto> Stops { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public double TotalDurationMin { get; set; }

    // Comparação com recolher e entregar uma encomenda de cada vez
    public double BaselineDistanceKm { get; set; }
    public double SavingPercent { get; set; }

    public string? GoogleMapsUrl { get; set; }
}

public class RouteStopDto
{
    public int Sequence { get; set; }
    public Guid OrderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
