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

public class GeocodingTests
{
    private readonly OrderDbContext _db;
    private readonly Mock<IOsrmService> _osrm = new();
    private readonly Mock<IGeocodingService> _geocoding = new();

    public GeocodingTests()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new OrderDbContext(options);

        _osrm.Setup(x => x.ObterRotaAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync((10.0, 15.0));
    }

    // Método auxiliar para criar o controlador com um Cliente autenticado
    private OrdersController CriarController()
    {
        var controller = new OrdersController(_db, _osrm.Object, new Mock<IIdentityServiceClient>().Object,
            NullLogger<OrdersController>.Instance, _geocoding.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Cliente")
                }, "Test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task CreateOrder_ComMoradas_CriaPontosForaDoCatalogo()
    {
        // 1. Preparação (Arrange)
        _geocoding.Setup(x => x.GeocodeAsync("Rua A 1, Braga", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult("Rua A, Braga, Portugal", 41.55, -8.42));
        _geocoding.Setup(x => x.GeocodeAsync("Rua B 2, Barcelos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodingResult("Rua B, Barcelos, Portugal", 41.53, -8.62));
        var dto = new CreateOrderDto { OriginAddress = "  Rua A   1, Braga ", DestinationAddress = "Rua B 2, Barcelos", Weight = 2 };

        // 2. Ação (Act)
        var result = await CriarController().CreateOrder(dto);

        // 3. Verificação (Assert)
        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(2, await _db.Points.CountAsync(p => !p.IsCatalog));
        Assert.True(await _db.Points.AnyAsync(p => p.Address == "Rua A 1, Braga")); // A morada é guardada já limpa
    }

    [Fact]
    public async Task CreateOrder_MoradaNaoEncontrada_Devolve422ENaoGuardaNada()
    {
        // 1. Preparação (Arrange)
        _geocoding.Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingResult?)null);
        var dto = new CreateOrderDto { OriginAddress = "Morada inventada 999", DestinationAddress = "Outra 1", Weight = 2 };

        // 2. Ação (Act)
        var result = await CriarController().CreateOrder(dto);

        // 3. Verificação (Assert)
        Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Empty(_db.Orders);
        Assert.Empty(_db.Points);
    }

    [Fact]
    public async Task CreateOrder_NominatimEmBaixo_Devolve503()
    {
        // 1. Preparação (Arrange)
        _geocoding.Setup(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));
        var dto = new CreateOrderDto { OriginAddress = "Rua A 1, Braga", DestinationAddress = "Rua B 2, Barcelos", Weight = 2 };

        // 2. Ação (Act)
        var result = await CriarController().CreateOrder(dto);

        // 3. Verificação (Assert)
        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, error.StatusCode);
        Assert.DoesNotContain("timeout", System.Text.Json.JsonSerializer.Serialize(error.Value)); // Sem detalhes internos
    }

    [Fact]
    public async Task CreateOrder_PontoEMoradaAoMesmoTempo_DevolveBadRequest()
    {
        // 1. Preparação (Arrange)
        var dto = new CreateOrderDto { OriginPointId = Guid.NewGuid(), OriginAddress = "Rua A 1, Braga", DestinationAddress = "Rua B 2", Weight = 2 };

        // 2. Ação (Act)
        var result = await CriarController().CreateOrder(dto);

        // 3. Verificação (Assert)
        Assert.IsType<BadRequestObjectResult>(result);
        _geocoding.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateOrder_MoradaRepetida_ReutilizaOPontoSemNovoPedido()
    {
        // 1. Preparação (Arrange)
        _db.Points.Add(new Point { Name = "Rua A, Braga", Address = "Rua A 1, Braga", Latitude = 41.55, Longitude = -8.42, IsCatalog = false });
        _db.Points.Add(new Point { Name = "Rua B, Barcelos", Address = "Rua B 2, Barcelos", Latitude = 41.53, Longitude = -8.62, IsCatalog = false });
        await _db.SaveChangesAsync();
        var dto = new CreateOrderDto { OriginAddress = "Rua A 1, Braga", DestinationAddress = "Rua B 2, Barcelos", Weight = 2 };

        // 2. Ação (Act)
        await CriarController().CreateOrder(dto);

        // 3. Verificação (Assert)
        _geocoding.Verify(x => x.GeocodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(2, await _db.Points.CountAsync());
    }

    [Fact]
    public async Task GetPoints_NaoDevolveMoradasDeClientes()
    {
        // 1. Preparação (Arrange)
        _db.Points.Add(new Point { Name = "Ponto IPCA", IsCatalog = true });
        _db.Points.Add(new Point { Name = "Casa de um cliente", Address = "Rua Privada 1", IsCatalog = false });
        await _db.SaveChangesAsync();

        // 2. Ação (Act)
        var result = await new PointsController(_db).GetPoints();

        // 3. Verificação (Assert)
        var ok = Assert.IsType<OkObjectResult>(result);
        var points = Assert.IsAssignableFrom<IEnumerable<PointDto>>(ok.Value);
        Assert.Single(points);
        Assert.Equal("Ponto IPCA", points.First().Name);
    }

    [Fact]
    public async Task Geocode_MoradaVazia_DevolveBadRequest()
    {
        // 1. Preparação (Arrange)
        var controller = new GeocodeController(_geocoding.Object, NullLogger<GeocodeController>.Instance);

        // 2. Ação (Act)
        var result = await controller.Geocode("   ", CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
