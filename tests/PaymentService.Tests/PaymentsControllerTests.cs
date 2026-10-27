using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaymentService.Controllers;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

public class PaymentsControllerTests
{
    private readonly PaymentDbContext _db;
    private readonly Mock<IOrderServiceClient> _orders = new();
    private readonly Mock<IStripeGateway> _stripe = new();

    public PaymentsControllerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new PaymentDbContext(options);
    }

    // Método auxiliar para criar o controlador com o utilizador autenticado
    private PaymentsController CriarController(string role, Guid userId)
    {
        var confirmation = new PaymentConfirmationService(_db, _orders.Object, NullLogger<PaymentConfirmationService>.Instance);
        var controller = new PaymentsController(_db, _orders.Object, _stripe.Object, confirmation, NullLogger<PaymentsController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }, "Test"))
            }
        };
        return controller;
    }

    // Método auxiliar para simular a encomenda devolvida pelo Order Service
    private static OrderSummaryDto Encomenda(Guid customerId, string paymentStatus = "Unpaid", string status = "Pending") => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        Price = 7.50m,
        Status = status,
        PaymentStatus = paymentStatus
    };

    [Fact]
    public async Task Checkout_EncomendaDeOutroCliente_DevolveForbid()
    {
        // 1. Preparação (Arrange)
        var order = Encomenda(Guid.NewGuid());
        _orders.Setup(o => o.GetOrderAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // 2. Ação (Act)
        var result = await CriarController("Cliente", Guid.NewGuid())
            .CreateCheckout(new CreateCheckoutDto { OrderId = order.Id }, CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<ForbidResult>(result);
        _stripe.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Checkout_EncomendaJaPaga_DevolveConflict()
    {
        // 1. Preparação (Arrange)
        var customerId = Guid.NewGuid();
        var order = Encomenda(customerId, paymentStatus: "Paid");
        _orders.Setup(o => o.GetOrderAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // 2. Ação (Act)
        var result = await CriarController("Cliente", customerId)
            .CreateCheckout(new CreateCheckoutDto { OrderId = order.Id }, CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Checkout_Valido_CriaPagamentoEmCentimosEDevolveUrl()
    {
        // 1. Preparação (Arrange)
        var customerId = Guid.NewGuid();
        var order = Encomenda(customerId);
        _orders.Setup(o => o.GetOrderAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _stripe.Setup(s => s.CreateCheckoutSessionAsync(It.IsAny<Guid>(), order.Id, 750, "eur",
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeCheckout("cs_test_123", "https://checkout.stripe.com/c/pay/cs_test_123"));

        // 2. Ação (Act)
        var result = await CriarController("Cliente", customerId)
            .CreateCheckout(new CreateCheckoutDto { OrderId = order.Id }, CancellationToken.None);

        // 3. Verificação (Assert)
        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var body = Assert.IsType<CheckoutResponseDto>(created.Value);
        Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_123", body.CheckoutUrl);

        var payment = Assert.Single(_db.Payments);
        Assert.Equal(750, payment.AmountCents);
        Assert.Equal(PaymentRecordStatus.Pending, payment.Status);
    }

    [Fact]
    public async Task ReturnSuccess_StripeConfirma_MarcaPagoEAvisaOrderService()
    {
        // 1. Preparação (Arrange)
        var payment = new Payment { OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), AmountCents = 750, StripeSessionId = "cs_test_abc" };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _stripe.Setup(s => s.GetSessionAsync("cs_test_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeSessionState("cs_test_abc", IsPaid: true, IsExpired: false, PaymentIntentId: "pi_test_1"));
        _orders.Setup(o => o.UpdatePaymentStatusAsync(payment.OrderId, "Paid", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // 2. Ação (Act)
        await CriarController("Cliente", payment.CustomerId).ReturnSuccess("cs_test_abc", CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.Equal(PaymentRecordStatus.Paid, payment.Status);
        Assert.Equal("pi_test_1", payment.StripePaymentIntentId);
        _orders.Verify(o => o.UpdatePaymentStatusAsync(payment.OrderId, "Paid", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refund_EncomendaNaoRejeitada_DevolveConflict()
    {
        // 1. Preparação (Arrange)
        var payment = new Payment
        {
            OrderId = Guid.NewGuid(), CustomerId = Guid.NewGuid(), AmountCents = 750,
            StripeSessionId = "cs_test_r", StripePaymentIntentId = "pi_test_r", Status = PaymentRecordStatus.Paid
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        _orders.Setup(o => o.GetOrderAsync(payment.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderSummaryDto { Id = payment.OrderId, CustomerId = payment.CustomerId, Status = "Validated", PaymentStatus = "Paid" });

        // 2. Ação (Act)
        var result = await CriarController("Gestor", Guid.NewGuid()).Refund(payment.OrderId, CancellationToken.None);

        // 3. Verificação (Assert)
        Assert.IsType<ConflictObjectResult>(result);
        _stripe.Verify(s => s.RefundAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
