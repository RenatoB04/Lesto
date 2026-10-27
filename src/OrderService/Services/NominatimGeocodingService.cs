using System.Globalization;
using System.Text.Json;

namespace OrderService.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    // A política de uso do Nominatim permite no máximo 1 pedido por segundo
    private static readonly SemaphoreSlim _rateLimit = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        // 1. Bloqueia outros pedidos concorrentes
        await _rateLimit.WaitAsync(cancellationToken);
        try
        {
            // 2. Garante o compasso de espera de 1 segundo entre pedidos
            var tempoDesdeUltimoPedido = DateTime.UtcNow - _lastRequest;
            if (tempoDesdeUltimoPedido.TotalSeconds < 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(1) - tempoDesdeUltimoPedido, cancellationToken);
            }

            // 3. Pesquisar a morada (só em Portugal, 1 resultado)
            var url = $"/search?q={Uri.EscapeDataString(address)}&format=jsonv2&limit=1&countrycodes=pt&accept-language=pt";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            _lastRequest = DateTime.UtcNow;

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Falha ao contactar o serviço de geocodificação.");

            // 4. Ler o primeiro resultado (lista vazia = morada não encontrada)
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                return null;

            var first = document.RootElement[0];
            var latitude = double.Parse(first.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture);
            var longitude = double.Parse(first.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture);
            var displayName = first.TryGetProperty("display_name", out var name) ? name.GetString() ?? address : address;

            return new GeocodingResult(displayName, latitude, longitude);
        }
        finally
        {
            // 5. Liberta o bloqueio para o próximo pedido avançar
            _rateLimit.Release();
        }
    }
}
