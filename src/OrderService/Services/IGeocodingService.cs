namespace OrderService.Services;

public interface IGeocodingService
{
    // Devolve null se a morada não for encontrada; lança exceção se o serviço falhar
    Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}

public record GeocodingResult(string DisplayName, double Latitude, double Longitude);
