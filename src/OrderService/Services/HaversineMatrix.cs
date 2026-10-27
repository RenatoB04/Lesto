namespace OrderService.Services;

// Matriz de distâncias e durações (Km e Minutos) entre pontos; Source indica a origem dos valores
public record RouteMatrix(double[,] DistanciasKm, double[,] DuracoesMin, string Source);

// Plano B quando o OSRM não responde: distância em linha reta corrigida para estrada
public static class HaversineMatrix
{
    // As estradas são em média ~30% mais longas que a linha reta
    public const double FatorEstrada = 1.3;
    public const double VelocidadeMediaKmh = 40.0;
    private const double RaioTerraKm = 6371.0;

    public static RouteMatrix Build(IReadOnlyList<(double Latitude, double Longitude)> pontos)
    {
        var n = pontos.Count;
        var distancias = new double[n, n];
        var duracoes = new double[n, n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var km = DistanciaKm(pontos[i].Latitude, pontos[i].Longitude, pontos[j].Latitude, pontos[j].Longitude) * FatorEstrada;
                distancias[i, j] = km;
                duracoes[i, j] = km / VelocidadeMediaKmh * 60.0;
            }
        }

        return new RouteMatrix(distancias, duracoes, "estimativa");
    }

    // Fórmula de Haversine: distância em linha reta entre duas coordenadas
    public static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double Rad(double graus) => graus * Math.PI / 180.0;

        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * RaioTerraKm * Math.Asin(Math.Sqrt(a));
    }
}
