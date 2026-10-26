namespace PaymentService.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }

    // Valor em cêntimos, como o Stripe espera (ex: 7,50 € = 750)
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "eur";

    public string StripeSessionId { get; set; } = string.Empty;
    public string? StripePaymentIntentId { get; set; }

    public PaymentRecordStatus Status { get; set; } = PaymentRecordStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
