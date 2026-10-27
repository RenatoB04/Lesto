using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Security;
using OrderService.Services;

namespace OrderService.Tests;

public class PaymentRulesTests
{
    // Método auxiliar para criar uma base de dados limpa na memória para cada teste
    private static OrderDbContext NovaBaseDeDados()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OrderDbContext(options);
    }

    [Fact]
    public void PricingCalculator_SomaBaseKmEKg()
    {
        // 1. Preparação (Arrange)
        // 2,50 € de base + 10 km x 0,40 € + 5 kg x 0,20 € = 7,50 €
        double km = 10, kg = 5;

        // 2. Ação (Act)
        var preco = PricingCalculator.Calculate(km, kg, 2.50m, 0.40m, 0.20m);

        // 3. Verificação (Assert)
        Assert.Equal(7.50m, preco);
    }

    [Fact]
    public void PricingCalculator_NuncaFicaAbaixoDoMinimoDoStripe()
    {
        // 1. Preparação (Arrange)
        // Todas as taxas a zero dariam um preço de 0 €
        double km = 0, kg = 0;

        // 2. Ação (Act)
        var preco = PricingCalculator.Calculate(km, kg, 0m, 0m, 0m);

        // 3. Verificação (Assert)
        Assert.Equal(PricingCalculator.MinimumPrice, preco);
    }

    [Fact]
    public async Task AssignOrder_EncomendaPorPagar_DevolveConflict()
    {
        // 1. Preparação (Arrange)
        var db = NovaBaseDeDados();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending, PaymentStatus = PaymentStatus.Unpaid });
        await db.SaveChangesAsync();

        var identity = new Mock<IIdentityServiceClient>();
        var controller = new OrdersController(db, new Mock<IOsrmService>().Object, identity.Object, NullLogger<OrdersController>.Instance, new Mock<IGeocodingService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, "Gestor")
                }, "Test"))
            }
        };

        // 2. Ação (Act)
        var result = await controller.AssignOrder(orderId, new AssignOrderDto { CourierId = Guid.NewGuid() });

        // 3. Verificação (Assert)
        Assert.IsType<ConflictObjectResult>(result);
        identity.Verify(x => x.IsCourierAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never); // O Identity Service nem chega a ser chamado
    }

    [Fact]
    public async Task InternalUpdatePayment_MarcaComoPagaEGuardaData()
    {
        // 1. Preparação (Arrange)
        var db = NovaBaseDeDados();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Pending });
        await db.SaveChangesAsync();

        var controller = new InternalOrdersController(db);

        // 2. Ação (Act)
        var result = await controller.UpdatePaymentStatus(orderId, new UpdatePaymentStatusDto { PaymentStatus = "Paid" }, CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<OkObjectResult>(result);
        var order = await db.Orders.FindAsync(orderId);
        Assert.Equal(PaymentStatus.Paid, order!.PaymentStatus);
        Assert.NotNull(order.PaidAt);
    }

    [Fact]
    public async Task InternalUpdatePayment_ReembolsoDeEncomendaPorPagar_DevolveConflict()
    {
        // 1. Preparação (Arrange)
        var db = NovaBaseDeDados();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order { Id = orderId, Status = OrderStatus.Rejected });
        await db.SaveChangesAsync();

        var controller = new InternalOrdersController(db);

        // 2. Ação (Act)
        var result = await controller.UpdatePaymentStatus(orderId, new UpdatePaymentStatusDto { PaymentStatus = "Refunded" }, CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void InternalApiKey_ChaveErrada_DevolveUnauthorized()
    {
        // 1. Preparação (Arrange)
        Environment.SetEnvironmentVariable("INTERNAL_API_KEY", "chave-de-teste");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[InternalApiKeyAttribute.HeaderName] = "chave-errada";
        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());

        // 2. Ação (Act)
        new InternalApiKeyAttribute().OnAuthorization(context);

        // 3. Verificação (Assert)
        Assert.IsType<UnauthorizedResult>(context.Result);
    }
}
