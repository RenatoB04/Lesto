using System.Text.Json;
using OrderService.DTOs;

namespace OrderService.Services;

public class OsrmService : IOsrmService
{
    private readonly HttpClient _httpClient;
    
    // Semáforo estático para garantir globalmente que só passa 1 pedido de cada vez
    private static readonly SemaphoreSlim _rateLimit = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public OsrmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(double DistanciaKm, double DuracaoMin)> ObterRotaAsync(double latOrigem, double lonOrigem, double latDestino, double lonDestino)
    {
        // 1. Bloqueia outros pedidos concorrentes
        await _rateLimit.WaitAsync();
        try
        {
            // 2. Garante o compasso de espera de 1 segundo entre pedidos
            var tempoDesdeUltimoPedido = DateTime.UtcNow - _lastRequest;
            if (tempoDesdeUltimoPedido.TotalSeconds < 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(1) - tempoDesdeUltimoPedido);
            }

            // O OSRM usa a ordem Longitude,Latitude 
            var url = $"http://router.project-osrm.org/route/v1/driving/{lonOrigem.ToString(System.Globalization.CultureInfo.InvariantCulture)},{latOrigem.ToString(System.Globalization.CultureInfo.InvariantCulture)};{lonDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)},{latDestino.ToString(System.Globalization.CultureInfo.InvariantCulture)}?overview=false";

            var response = await _httpClient.GetAsync(url);
            
            // Regista o momento em que fizemos o pedido
            _lastRequest = DateTime.UtcNow;

            if (!response.IsSuccessStatusCode)
                throw new Exception("Falha ao contactar o servidor OSRM.");

            var content = await response.Content.ReadAsStringAsync();
            var osrmResult = JsonSerializer.Deserialize<OsrmResponse>(content);

            if (osrmResult == null || osrmResult.Code != "Ok" || !osrmResult.Routes.Any())
                throw new Exception("Não foi possível calcular a rota.");

            var rota = osrmResult.Routes.First();

            // 3. Converter Metros para Km e Segundos para Minutos
            var distanciaKm = rota.Distance / 1000.0;
            var duracaoMin = rota.Duration / 60.0;

            return (Math.Round(distanciaKm, 2), Math.Round(duracaoMin, 2));
        }
        finally
        {
            // 4. Liberta o bloqueio para o próximo pedido avançar
            _rateLimit.Release();
        }
    }
}