using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Estafeta")]
public class RoutesController : ControllerBase
{
    // O servidor público do OSRM limita o tamanho da matriz; 40 paragens chegam para um estafeta
    public const int MaxStops = 40;

    // O Google Maps aceita no máximo 9 pontos intermédios no link
    private const int MaxGoogleMapsWaypoints = 9;

    private readonly OrderDbContext _context;
    private readonly IOsrmService _osrmService;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(OrderDbContext context, IOsrmService osrmService, ILogger<RoutesController> logger)
    {
        _context = context;
        _osrmService = osrmService;
        _logger = logger;
    }

    // Obter a rota otimizada das encomendas atribuídas ao estafeta autenticado
    [HttpGet("optimized")]
    public async Task<IActionResult> GetOptimizedRoute([FromQuery] double? lat, [FromQuery] double? lon)
    {
        // 1. Obter o ID do estafeta a partir do Token JWT
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid courierId)) return Unauthorized();

        if (lat.HasValue != lon.HasValue || lat is < -90 or > 90 || lon is < -180 or > 180)
            return BadRequest(new { Message = "Indique a posição atual com lat e lon válidos (ou nenhum dos dois)." });

        // 2. Encomendas por recolher (Validated) e em trânsito (InTransit)
        var orders = await _context.Orders
            .Include(o => o.Origin)
            .Include(o => o.Destination)
            .Where(o => o.CourierId == courierId &&
                        (o.Status == OrderStatus.Validated || o.Status == OrderStatus.InTransit))
            .OrderBy(o => o.Id)
            .ToListAsync();

        // 3. Montar as paragens pela ordem "ingénua": uma encomenda de cada vez
        var paragens = new List<RouteStop>();
        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.Validated)
                paragens.Add(new RouteStop(order.Id, StopType.Pickup, order.Origin.Name, order.Origin.Latitude, order.Origin.Longitude));
            paragens.Add(new RouteStop(order.Id, StopType.Delivery, order.Destination.Name, order.Destination.Latitude, order.Destination.Longitude));
        }

        if (paragens.Count == 0)
            return Ok(new OptimizedRouteDto { Source = "osrm" });

        if (paragens.Count > MaxStops)
            return BadRequest(new { Message = $"A rota tem demasiadas paragens (máximo {MaxStops})." });

        // 4. Ponto de partida: a posição atual ou, se não for indicada, a primeira paragem
        var partida = lat.HasValue ? (lat.Value, lon!.Value) : (paragens[0].Latitude, paragens[0].Longitude);
        var pontos = new List<(double Latitude, double Longitude)> { partida };
        pontos.AddRange(paragens.Select(p => (p.Latitude, p.Longitude)));

        // 5. Matriz de distâncias (OSRM, ou estimativa se o OSRM falhar)
        RouteMatrix matriz;
        try
        {
            matriz = await _osrmService.ObterMatrizAsync(pontos);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM table unavailable, using Haversine estimate.");
            matriz = HaversineMatrix.Build(pontos);
        }

        // 6. Otimizar e comparar com a ordem ingénua
        var rotaIngenua = Enumerable.Range(0, paragens.Count).ToList();
        var rota = RouteOptimizer.Optimize(matriz.DistanciasKm, paragens, rotaIngenua);

        var distancia = RouteOptimizer.Cost(matriz.DistanciasKm, rota);
        var duracao = RouteOptimizer.Cost(matriz.DuracoesMin, rota);
        var distanciaIngenua = RouteOptimizer.Cost(matriz.DistanciasKm, rotaIngenua);
        var poupanca = distanciaIngenua > 0 ? (distanciaIngenua - distancia) / distanciaIngenua * 100 : 0;

        return Ok(new OptimizedRouteDto
        {
            Source = matriz.Source,
            Stops = rota.Select((indice, posicao) => new RouteStopDto
            {
                Sequence = posicao + 1,
                OrderId = paragens[indice].OrderId,
                Type = paragens[indice].Type == StopType.Pickup ? "Recolha" : "Entrega",
                Name = paragens[indice].Name,
                Latitude = paragens[indice].Latitude,
                Longitude = paragens[indice].Longitude
            }).ToList(),
            TotalDistanceKm = Math.Round(distancia, 2),
            TotalDurationMin = Math.Round(duracao, 1),
            BaselineDistanceKm = Math.Round(distanciaIngenua, 2),
            SavingPercent = Math.Round(poupanca, 1),
            GoogleMapsUrl = CriarLinkGoogleMaps(partida, rota.Select(i => paragens[i]).ToList())
        });
    }

    // Método auxiliar para criar o link de navegação do Google Maps (não precisa de chave de API)
    public static string? CriarLinkGoogleMaps((double Latitude, double Longitude) partida, IReadOnlyList<RouteStop> paragens)
    {
        if (paragens.Count == 0 || paragens.Count - 1 > MaxGoogleMapsWaypoints) return null;

        static string Coord(double lat, double lon) =>
            $"{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}";

        var destino = paragens[paragens.Count - 1];
        var url = $"https://www.google.com/maps/dir/?api=1&travelmode=driving" +
                  $"&origin={Coord(partida.Latitude, partida.Longitude)}" +
                  $"&destination={Coord(destino.Latitude, destino.Longitude)}";

        // Paragens seguidas no mesmo local (ex: duas recolhas no mesmo ponto) só aparecem uma vez
        var destinoCoord = Coord(destino.Latitude, destino.Longitude);
        var intermedios = new List<string>();
        foreach (var coord in paragens.Take(paragens.Count - 1).Select(p => Coord(p.Latitude, p.Longitude)))
        {
            if (intermedios.Count == 0 || intermedios[^1] != coord) intermedios.Add(coord);
        }
        if (intermedios.Count > 0 && intermedios[^1] == destinoCoord) intermedios.RemoveAt(intermedios.Count - 1);
        if (intermedios.Count > 0)
            url += "&waypoints=" + Uri.EscapeDataString(string.Join("|", intermedios));

        return url;
    }
}
