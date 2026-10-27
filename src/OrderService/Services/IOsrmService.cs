namespace OrderService.Services;

public interface IOsrmService
{
    // Devolve um Tuplo com a distância em Km e a duração em Minutos
    Task<(double DistanciaKm, double DuracaoMin)> ObterRotaAsync(double latOrigem, double lonOrigem, double latDestino, double lonDestino);

    // Devolve as matrizes de distâncias (Km) e durações (Minutos) entre todos os pontos
    Task<RouteMatrix> ObterMatrizAsync(IReadOnlyList<(double Latitude, double Longitude)> pontos);
}