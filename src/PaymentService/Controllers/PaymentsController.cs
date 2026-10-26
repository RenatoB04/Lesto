using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly IOrderServiceClient _orders;
    private readonly IStripeGateway _stripe;
    private readonly PaymentConfirmationService _confirmation;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentDbContext context,
        IOrderServiceClient orders,
        IStripeGateway stripe,
        PaymentConfirmationService confirmation,
        ILogger<PaymentsController> logger)
    {
        _context = context;
        _orders = orders;
        _stripe = stripe;
        _confirmation = confirmation;
        _logger = logger;
    }

    // Criar o pagamento de uma encomenda (devolve o URL da página do Stripe)
    [HttpPost("checkout")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutDto dto, CancellationToken cancellationToken)
    {
        // 1. Obter o ID do utilizador autenticado a partir do Token JWT
        if (!TryGetUserId(out var customerId)) return Unauthorized();

        // 2. Obter a encomenda ao Order Service
        OrderSummaryDto? order;
        try
        {
            order = await _orders.GetOrderAsync(dto.OrderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao contactar o Order Service.");
            return StatusCode(503, new { Message = "Serviço de encomendas indisponível. Tente mais tarde." });
        }

        // 3. Validar a encomenda (tem de ser do cliente, pendente e por pagar)
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });
        if (order.CustomerId != customerId) return Forbid();
        if (order.PaymentStatus != "Unpaid")
            return Conflict(new { Message = "Esta encomenda já foi paga." });
        if (order.Status != "Pending")
            return Conflict(new { Message = "Só é possível pagar encomendas pendentes." });

        // 4. Criar a sessão de pagamento no Stripe (valor em cêntimos)
        var payment = new Payment
        {
            OrderId = order.Id,
            CustomerId = customerId,
            AmountCents = (long)Math.Round(order.Price * 100m, MidpointRounding.AwayFromZero),
            Currency = "eur"
        };

        var baseUrl = PublicBaseUrl();
        StripeCheckout checkout;
        try
        {
            checkout = await _stripe.CreateCheckoutSessionAsync(
                payment.Id, order.Id, payment.AmountCents, payment.Currency,
                successUrl: $"{baseUrl}/api/payments/return/success?session_id={{CHECKOUT_SESSION_ID}}",
                cancelUrl: $"{baseUrl}/api/payments/return/cancel",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao criar a sessão de pagamento no Stripe.");
            return StatusCode(503, new { Message = "Serviço de pagamentos indisponível. Tente mais tarde." });
        }

        // 5. Guardar o pagamento como pendente
        payment.StripeSessionId = checkout.SessionId;
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new CheckoutResponseDto
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.AmountCents / 100m,
            Currency = payment.Currency,
            CheckoutUrl = checkout.Url
        });
    }

    // Consultar o pagamento de uma encomenda
    [HttpGet("{orderId:guid}")]
    [Authorize(Roles = "Cliente, Gestor, Administrador")]
    public async Task<IActionResult> GetPayment(Guid orderId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var payment = await LatestPaymentAsync(orderId, cancellationToken);
        if (payment == null) return NotFound(new { Message = "Não existe pagamento para esta encomenda." });

        if (User.IsInRole("Cliente") && payment.CustomerId != userId) return Forbid();

        // Se ainda estiver pendente, confirmar junto do Stripe (caso o webhook não tenha chegado)
        if (payment.Status == PaymentRecordStatus.Pending)
        {
            try
            {
                var state = await _stripe.GetSessionAsync(payment.StripeSessionId, cancellationToken);
                await _confirmation.ApplySessionStateAsync(payment, state, cancellationToken);
            }
            catch (Exception ex)
            {
                // Se o Stripe falhar, devolve o último estado conhecido
                _logger.LogWarning(ex, "Não foi possível consultar o Stripe para o pagamento {PaymentId}.", payment.Id);
            }
        }

        return Ok(ToDto(payment));
    }

    // Reembolsar uma encomenda rejeitada que já tinha sido paga
    [HttpPost("{orderId:guid}/refund")]
    [Authorize(Roles = "Gestor, Administrador")]
    public async Task<IActionResult> Refund(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .Where(p => p.OrderId == orderId && p.Status == PaymentRecordStatus.Paid)
            .FirstOrDefaultAsync(cancellationToken);
        if (payment == null) return Conflict(new { Message = "Não existe um pagamento confirmado para esta encomenda." });

        // Confirmar no Order Service que a encomenda foi rejeitada
        OrderSummaryDto? order;
        try
        {
            order = await _orders.GetOrderAsync(orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao contactar o Order Service.");
            return StatusCode(503, new { Message = "Serviço de encomendas indisponível. Tente mais tarde." });
        }
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });
        if (order.Status != "Rejected")
            return Conflict(new { Message = "Só é possível reembolsar encomendas rejeitadas." });
        if (string.IsNullOrEmpty(payment.StripePaymentIntentId))
            return Conflict(new { Message = "O pagamento não tem referência no Stripe para reembolso." });

        try
        {
            await _stripe.RefundAsync(payment.StripePaymentIntentId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao reembolsar o pagamento {PaymentId} no Stripe.", payment.Id);
            return StatusCode(503, new { Message = "Não foi possível efetuar o reembolso. Tente mais tarde." });
        }

        // Atualizar o pagamento e avisar o Order Service
        payment.Status = PaymentRecordStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _orders.UpdatePaymentStatusAsync(orderId, "Refunded", cancellationToken);

        return Ok(new { Message = "Pagamento reembolsado com sucesso.", Payment = ToDto(payment) });
    }

    // Webhook do Stripe (sem JWT; a assinatura do pedido é verificada)
    [HttpPost("webhook")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var secret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
        if (string.IsNullOrWhiteSpace(secret))
            return StatusCode(503, new { Message = "Webhook não configurado." });

        // 1. Ler o pedido e validar a assinatura do Stripe
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = Stripe.EventUtility.ConstructEvent(
                json, Request.Headers["Stripe-Signature"], secret, throwOnApiVersionMismatch: false);
        }
        catch (Stripe.StripeException)
        {
            // Assinatura inválida, o pedido não veio do Stripe
            return BadRequest();
        }

        // 2. Atualizar o pagamento nos eventos de sessão concluída ou expirada
        if (stripeEvent.Data.Object is Stripe.Checkout.Session session &&
            (stripeEvent.Type == "checkout.session.completed" ||
             stripeEvent.Type == "checkout.session.async_payment_succeeded" ||
             stripeEvent.Type == "checkout.session.expired"))
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id, cancellationToken);

            if (payment != null)
            {
                var state = new StripeSessionState(
                    session.Id,
                    IsPaid: session.PaymentStatus == "paid",
                    IsExpired: stripeEvent.Type == "checkout.session.expired",
                    PaymentIntentId: session.PaymentIntentId);
                await _confirmation.ApplySessionStateAsync(payment, state, cancellationToken);
            }
        }

        // 3. Responder 200 mesmo a eventos ignorados, para o Stripe não os reenviar
        return Ok();
    }

    // Página de regresso do Stripe após o pagamento (também confirma o pagamento)
    [HttpGet("return/success")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ReturnSuccess([FromQuery(Name = "session_id")] string? sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest("Sessão em falta.");

        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == sessionId, cancellationToken);
        if (payment == null) return NotFound("Pagamento não encontrado.");

        try
        {
            // O estado é sempre confirmado no Stripe, não basta o session_id do URL
            var state = await _stripe.GetSessionAsync(sessionId, cancellationToken);
            await _confirmation.ApplySessionStateAsync(payment, state, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível confirmar a sessão {SessionId} no regresso do Stripe.", sessionId);
        }

        return payment.Status == PaymentRecordStatus.Paid
            ? Content($"Pagamento confirmado para a encomenda {payment.OrderId}. Pode fechar esta janela.")
            : Content("Pagamento ainda não confirmado. Consulte o estado mais tarde.");
    }

    // Página de regresso quando o cliente cancela o pagamento
    [HttpGet("return/cancel")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult ReturnCancel()
    {
        return Content("Pagamento cancelado. A encomenda continua por pagar.");
    }

    // Métodos auxiliares para evitar duplicação de código
    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }

    private Task<Payment?> LatestPaymentAsync(Guid orderId, CancellationToken cancellationToken) =>
        _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.Status == PaymentRecordStatus.Paid || p.Status == PaymentRecordStatus.Refunded)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static string PublicBaseUrl() =>
        (Environment.GetEnvironmentVariable("PUBLIC_BASE_URL") ?? "http://localhost:5000").TrimEnd('/');

    private static PaymentDto ToDto(Payment p) => new()
    {
        PaymentId = p.Id,
        OrderId = p.OrderId,
        Amount = p.AmountCents / 100m,
        Currency = p.Currency,
        Status = p.Status.ToString(),
        CreatedAt = p.CreatedAt,
        PaidAt = p.PaidAt,
        RefundedAt = p.RefundedAt
    };
}
