using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;
using Xunit;

namespace OrderService.Tests;

public class OrdersControllerTests
{
    // Método auxiliar para criar uma base de dados limpa na memória para cada teste
    private OrderDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Um GUID garante que cada teste tem a sua BD isolada
            .Options;
        
        return new OrderDbContext(options);
    }

    // Método auxiliar para simular o Token JWT e injetar o Utilizador no Controlador
    private void SetUserContext(OrdersController controller, string role, Guid userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuthType"));
        
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task PickupOrder_EstafetaTentaRecolherEncomendaDeOutro_DevolveForbid()
    {
        // 1. Preparação (Arrange)
        var dbContext = GetInMemoryDbContext();
        var mockOsrm = new Mock<IOsrmService>();
        var mockIdentity = new Mock<IIdentityServiceClient>();

        var orderId = Guid.NewGuid();
        var estafetaDonoId = Guid.NewGuid();
        var estafetaIntrusoId = Guid.NewGuid();

        // Inserir a encomenda na BD em memória atribuída ao "Estafeta Dono"
        dbContext.Orders.Add(new Order 
        { 
            Id = orderId, 
            CourierId = estafetaDonoId, 
            Status = OrderStatus.Validated 
        });
        await dbContext.SaveChangesAsync();

        var controller = new OrdersController(dbContext, mockOsrm.Object, mockIdentity.Object);
        
        // Simular que o pedido está a ser feito pelo "Estafeta Intruso"
        SetUserContext(controller, "Estafeta", estafetaIntrusoId);

        // 2. Ação (Act)
        var result = await controller.PickupOrder(orderId);

        // 3. Verificação (Assert)
        Assert.IsType<ForbidResult>(result); // Tem de ser bloqueado
    }

    [Fact]
    public async Task CreateOrder_PesoExcedeLimite_DevolveBadRequest()
    {
        // 1. Preparação (Arrange)
        var dbContext = GetInMemoryDbContext();
        var mockOsrm = new Mock<IOsrmService>();
        var mockIdentity = new Mock<IIdentityServiceClient>();

        // Configurar a regra de peso máximo para 30kg
        dbContext.Settings.Add(new Setting { Key = "PesoMaximoKg", Value = "30" });
        await dbContext.SaveChangesAsync();

        var controller = new OrdersController(dbContext, mockOsrm.Object, mockIdentity.Object);
        SetUserContext(controller, "Cliente", Guid.NewGuid());

        // Cliente tenta enviar 35kg
        var dto = new CreateOrderDto { Weight = 35 }; 

        // 2. Ação (Act)
        var result = await controller.CreateOrder(dto);

        // 3. Verificação (Assert)
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        
        // Validar que a resposta não é nula
        Assert.NotNull(badRequestResult.Value); 
    }

    [Fact]
    public async Task PickupOrder_TransicaoInvalida_DevolveConflict()
    {
        // 1. Preparação
        var dbContext = GetInMemoryDbContext();
        var mockOsrm = new Mock<IOsrmService>();
        var mockIdentity = new Mock<IIdentityServiceClient>();
        var estafetaId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // Encomenda já entregue (não pode ser recolhida novamente)
        dbContext.Orders.Add(new Order { Id = orderId, CourierId = estafetaId, Status = OrderStatus.Delivered });
        await dbContext.SaveChangesAsync();

        var controller = new OrdersController(dbContext, mockOsrm.Object, mockIdentity.Object);
        SetUserContext(controller, "Estafeta", estafetaId);

        // 2. Ação
        var result = await controller.PickupOrder(orderId);

        // 3. Verificação
        Assert.IsType<ConflictObjectResult>(result); // Valida o HTTP 409 exigido
    }

    [Fact]
    public async Task GetOrders_EstafetaApenasVeAsSuasEncomendas()
    {
        var dbContext = GetInMemoryDbContext();
        var estafetaA = Guid.NewGuid();
        var estafetaB = Guid.NewGuid();

        // 1. Criar um ponto fictício para satisfazer as regras da Base de Dados
        var ponto = new Point { Id = Guid.NewGuid(), Name = "Sede", Latitude = 0, Longitude = 0 };
        dbContext.Points.Add(ponto);

        // 2. Associar o ponto às encomendas para que o Include() não as esconda
        dbContext.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), CourierId = estafetaA, Origin = ponto, Destination = ponto },
            new Order { Id = Guid.NewGuid(), CourierId = estafetaB, Origin = ponto, Destination = ponto }
        );
        await dbContext.SaveChangesAsync();

        var controller = new OrdersController(dbContext, new Mock<IOsrmService>().Object, new Mock<IIdentityServiceClient>().Object);
        SetUserContext(controller, "Estafeta", estafetaA);

        var result = await controller.GetOrders(null) as OkObjectResult;
        var orders = result?.Value as IEnumerable<OrderDto>;

        Assert.NotNull(orders);
        Assert.Single(orders); // O Estafeta A só deve ver 1 encomenda
    }

    [Fact]
    public async Task AssignOrder_UtilizadorSemRoleEstafeta_DevolveBadRequest()
    {
        var dbContext = GetInMemoryDbContext();
        var mockIdentity = new Mock<IIdentityServiceClient>();
        var orderId = Guid.NewGuid();

        dbContext.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending });
        await dbContext.SaveChangesAsync();

        // Simular que o Identity Service diz que o ID não é um Estafeta
        mockIdentity.Setup(x => x.IsCourierAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(false);

        var controller = new OrdersController(dbContext, new Mock<IOsrmService>().Object, mockIdentity.Object);
        SetUserContext(controller, "Gestor", Guid.NewGuid());
        controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = "Bearer tokenFalso";

        var result = await controller.AssignOrder(orderId, new AssignOrderDto { CourierId = Guid.NewGuid() });

        Assert.IsType<BadRequestObjectResult>(result); // Valida o HTTP 400
    }

    [Fact]
    public async Task CreateOrder_OSRMDevolveSucesso_GuardaEncomenda()
    {
        var dbContext = GetInMemoryDbContext();
        var mockOsrm = new Mock<IOsrmService>();
        
        var origemId = Guid.NewGuid();
        var destinoId = Guid.NewGuid();
        dbContext.Points.AddRange(
            new Point { Id = origemId, Latitude = 10, Longitude = 10 },
            new Point { Id = destinoId, Latitude = 20, Longitude = 20 }
        );
        dbContext.Settings.Add(new Setting { Key = "PesoMaximoKg", Value = "30" });
        await dbContext.SaveChangesAsync();

        // Simular o serviço OSRM a devolver distância e tempo
        mockOsrm.Setup(x => x.ObterRotaAsync(10, 10, 20, 20)).ReturnsAsync((15.5, 20.0));

        var controller = new OrdersController(dbContext, mockOsrm.Object, new Mock<IIdentityServiceClient>().Object);
        SetUserContext(controller, "Cliente", Guid.NewGuid());

        var dto = new CreateOrderDto { OriginPointId = origemId, DestinationPointId = destinoId, Weight = 5 };

        var result = await controller.CreateOrder(dto);
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, createdResult.StatusCode); // HTTP 201 Created
    }
}