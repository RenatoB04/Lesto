using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Tests;

public class RouteOptimizerTests
{
    // Método auxiliar para gerar um cenário aleatório (sempre igual para a mesma semente)
    private static (double[,] Matriz, List<RouteStop> Paragens) CenarioAleatorio(int semente, int encomendas, int emTransito = 0)
    {
        var random = new Random(semente);
        var paragens = new List<RouteStop>();
        for (var e = 0; e < encomendas; e++)
        {
            var id = Guid.NewGuid();
            if (e >= emTransito)
                paragens.Add(new RouteStop(id, StopType.Pickup, $"R{e}", random.NextDouble() * 10, random.NextDouble() * 10));
            paragens.Add(new RouteStop(id, StopType.Delivery, $"E{e}", random.NextDouble() * 10, random.NextDouble() * 10));
        }

        // Índice 0 = ponto de partida; distância euclidiana entre pontos
        var pontos = new List<(double X, double Y)> { (random.NextDouble() * 10, random.NextDouble() * 10) };
        pontos.AddRange(paragens.Select(p => (p.Latitude, p.Longitude)));
        var n = pontos.Count;
        var matriz = new double[n, n];
        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
                matriz[i, j] = Math.Sqrt(Math.Pow(pontos[i].X - pontos[j].X, 2) + Math.Pow(pontos[i].Y - pontos[j].Y, 2));

        return (matriz, paragens);
    }

    // Método auxiliar para calcular a melhor rota possível testando todas as ordens
    private static double OtimoPorForcaBruta(double[,] matriz, List<RouteStop> paragens)
    {
        var melhor = double.MaxValue;
        void Permutar(List<int> rota, bool[] usado)
        {
            if (rota.Count == paragens.Count)
            {
                if (RouteOptimizer.RespeitaPrecedencia(paragens, rota))
                    melhor = Math.Min(melhor, RouteOptimizer.Cost(matriz, rota));
                return;
            }
            for (var i = 0; i < paragens.Count; i++)
            {
                if (usado[i]) continue;
                usado[i] = true; rota.Add(i);
                Permutar(rota, usado);
                rota.RemoveAt(rota.Count - 1); usado[i] = false;
            }
        }
        Permutar(new List<int>(), new bool[paragens.Count]);
        return melhor;
    }

    [Fact]
    public void Optimize_NuncaEntregaAntesDeRecolher()
    {
        for (var semente = 0; semente < 50; semente++)
        {
            // 1. Preparação (Arrange)
            var (matriz, paragens) = CenarioAleatorio(semente, encomendas: 5, emTransito: 1);

            // 2. Ação (Act)
            var rota = RouteOptimizer.Optimize(matriz, paragens);

            // 3. Verificação (Assert)
            Assert.True(RouteOptimizer.RespeitaPrecedencia(paragens, rota));
            Assert.Equal(Enumerable.Range(0, paragens.Count), rota.OrderBy(i => i)); // Todas as paragens, uma vez cada
        }
    }

    [Fact]
    public void TwoOpt_NuncaPioraARotaDoVizinhoMaisProximo()
    {
        for (var semente = 0; semente < 50; semente++)
        {
            // 1. Preparação (Arrange)
            var (matriz, paragens) = CenarioAleatorio(semente, encomendas: 6);
            var vizinho = RouteOptimizer.NearestNeighbor(matriz, paragens);

            // 2. Ação (Act)
            var otimizada = RouteOptimizer.TwoOpt(matriz, paragens, vizinho);

            // 3. Verificação (Assert)
            Assert.True(RouteOptimizer.Cost(matriz, otimizada) <= RouteOptimizer.Cost(matriz, vizinho) + 1e-9);
        }
    }

    [Fact]
    public void Optimize_FicaPertoDoOtimoCalculadoPorForcaBruta()
    {
        // 1. Preparação (Arrange)
        var razoes = new List<double>();

        // 2. Ação (Act)
        for (var semente = 0; semente < 20; semente++)
        {
            var (matriz, paragens) = CenarioAleatorio(semente, encomendas: 3); // 6 paragens = 720 ordens possíveis
            var heuristica = RouteOptimizer.Cost(matriz, RouteOptimizer.Optimize(matriz, paragens));
            razoes.Add(heuristica / OtimoPorForcaBruta(matriz, paragens));
        }

        // 3. Verificação (Assert)
        Assert.All(razoes, r => Assert.True(r >= 1 - 1e-9)); // Nunca melhor que o ótimo (confirma o cálculo)
        Assert.True(razoes.Average() <= 1.15, $"Em média {razoes.Average():P1} do ótimo");
        Assert.True(razoes.Max() <= 1.5, $"No pior caso {razoes.Max():P1} do ótimo");
    }

    [Fact]
    public void Haversine_BragaBarcelos_CercaDe17Km()
    {
        // 1. Preparação (Arrange)
        double latBraga = 41.5503, lonBraga = -8.4200, latBarcelos = 41.5360, lonBarcelos = -8.6251;

        // 2. Ação (Act)
        var km = HaversineMatrix.DistanciaKm(latBraga, lonBraga, latBarcelos, lonBarcelos);

        // 3. Verificação (Assert)
        Assert.InRange(km, 16.0, 18.5);
    }

    [Fact]
    public void LinkGoogleMaps_DemasiadasParagens_DevolveNull()
    {
        // 1. Preparação (Arrange)
        var paragens = Enumerable.Range(0, 11)
            .Select(i => new RouteStop(Guid.NewGuid(), StopType.Delivery, $"P{i}", 41.5, -8.4))
            .ToList();

        // 2. Ação (Act)
        var link = RoutesController.CriarLinkGoogleMaps((41.5, -8.4), paragens);

        // 3. Verificação (Assert)
        Assert.Null(link);
    }

    [Fact]
    public async Task GetOptimizedRoute_OsrmEmBaixo_UsaEstimativaERespeitaPrecedencia()
    {
        // 1. Preparação (Arrange)
        var options = new DbContextOptionsBuilder<OrderDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new OrderDbContext(options);
        var courierId = Guid.NewGuid();
        var braga = new Point { Name = "Braga", Latitude = 41.5503, Longitude = -8.4200, IsCatalog = true };
        var barcelos = new Point { Name = "Barcelos", Latitude = 41.5360, Longitude = -8.6251, IsCatalog = true };
        var guimaraes = new Point { Name = "Guimarães", Latitude = 41.4444, Longitude = -8.2961, IsCatalog = true };
        db.Orders.Add(new Order { CourierId = courierId, Status = OrderStatus.Validated, Origin = braga, Destination = barcelos });
        db.Orders.Add(new Order { CourierId = courierId, Status = OrderStatus.Validated, Origin = guimaraes, Destination = braga });
        db.Orders.Add(new Order { CourierId = Guid.NewGuid(), Status = OrderStatus.Validated, Origin = braga, Destination = guimaraes }); // De outro estafeta
        await db.SaveChangesAsync();

        var osrm = new Mock<IOsrmService>();
        osrm.Setup(x => x.ObterMatrizAsync(It.IsAny<IReadOnlyList<(double, double)>>())).ThrowsAsync(new HttpRequestException());

        var controller = new RoutesController(db, osrm.Object, NullLogger<RoutesController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, courierId.ToString()),
                    new Claim(ClaimTypes.Role, "Estafeta")
                }, "Test"))
            }
        };

        // 2. Ação (Act)
        var result = await controller.GetOptimizedRoute(null, null);

        // 3. Verificação (Assert)
        var ok = Assert.IsType<OkObjectResult>(result);
        var rota = Assert.IsType<OptimizedRouteDto>(ok.Value);
        Assert.Equal("estimativa", rota.Source);
        Assert.Equal(4, rota.Stops.Count); // Só as 2 encomendas deste estafeta
        foreach (var orderId in rota.Stops.Select(s => s.OrderId).Distinct())
        {
            var recolha = rota.Stops.First(s => s.OrderId == orderId && s.Type == "Recolha").Sequence;
            var entrega = rota.Stops.First(s => s.OrderId == orderId && s.Type == "Entrega").Sequence;
            Assert.True(recolha < entrega);
        }
        Assert.True(rota.TotalDistanceKm <= rota.BaselineDistanceKm + 0.01);
    }
}
