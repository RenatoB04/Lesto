namespace OrderService.Models;

public class Point
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Pontos do catálogo (true) ou criados a partir da morada de um cliente (false)
    public bool IsCatalog { get; set; }
    public string? Address { get; set; }
}