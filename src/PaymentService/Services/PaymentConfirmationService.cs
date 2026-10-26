using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Services;

// Confirmação de pagamentos (usada pelo webhook, pela página de regresso e pela consulta)
public class PaymentConfirmationService
{
    private readonly PaymentDbContext _context;
    private readonly IOrderServiceClient _orders;
    private readonly ILogger<PaymentConfirmationService> _logger;

    public PaymentConfirmationService(PaymentDbContext context, IOrderServiceClient orders, ILogger<PaymentConfirmationService> logger)
    {
        _context = context;
        _orders = orders;
        _logger = logger;
    }

    // Atualizar o pagamento com o estado que o Stripe devolve
    public async Task ApplySessionStateAsync(Payment payment, StripeSessionState state, CancellationToken cancellationToken = default)
    {
        if (state.IsPaid)
        {
            await MarkPaidAsync(payment, state.PaymentIntentId, cancellationToken);
        }
        else if (state.IsExpired && payment.Status == PaymentRecordStatus.Pending)
        {
            payment.Status = PaymentRecordStatus.Expired;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkPaidAsync(Payment payment, string? paymentIntentId, CancellationToken cancellationToken = default)
    {
        if (payment.Status != PaymentRecordStatus.Paid)
        {
            payment.Status = PaymentRecordStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.StripePaymentIntentId ??= paymentIntentId;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Avisar sempre o Order Service (se uma chamada anterior falhou, é repetida)
        var ok = await _orders.UpdatePaymentStatusAsync(payment.OrderId, "Paid", cancellationToken);
        if (!ok)
        {
            _logger.LogWarning("O Order Service não aceitou a confirmação do pagamento {PaymentId} da encomenda {OrderId}.",
                payment.Id, payment.OrderId);
        }
    }
}
