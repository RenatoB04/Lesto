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
        }));
        
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
}