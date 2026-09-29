namespace OrderService.Services;

public interface IOsrmService
{
    // Devolve um Tuplo com a distância em Km e a duração em Minutos
    Task<(double DistanciaKm, double DuracaoMin)> ObterRotaAsync(double latOrigem, double lonOrigem, double latDestino, double lonDestino);
}