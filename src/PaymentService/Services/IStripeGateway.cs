namespace PaymentService.Services;

// Interface para o Stripe (permite simular o Stripe nos testes)
public interface IStripeGateway
{
    Task<StripeCheckout> CreateCheckoutSessionAsync(Guid paymentId, Guid orderId, long amountCents, string currency,
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default);

    Task<StripeSessionState> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task RefundAsync(string paymentIntentId, CancellationToken cancellationToken = default);
}

public record StripeCheckout(string SessionId, string Url);

public record StripeSessionState(string SessionId, bool IsPaid, bool IsExpired, string? PaymentIntentId);
