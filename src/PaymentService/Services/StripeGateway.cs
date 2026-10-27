using Stripe;
using Stripe.Checkout;

namespace PaymentService.Services;

public class StripeGateway : IStripeGateway
{
    private readonly IStripeClient _client;

    public StripeGateway(IStripeClient client)
    {
        _client = client;
    }

    public async Task<StripeCheckout> CreateCheckoutSessionAsync(Guid paymentId, Guid orderId, long amountCents, string currency,
        string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = orderId.ToString(),
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Entrega Lesto",
                            Description = $"Encomenda {orderId}"
                        }
                    }
                }
            },
            // Guardar os IDs para identificar o pagamento quando chega o webhook
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = paymentId.ToString(),
                ["orderId"] = orderId.ToString()
            }
        };

        var session = await new SessionService(_client).CreateAsync(options, cancellationToken: cancellationToken);
        return new StripeCheckout(session.Id, session.Url);
    }

    public async Task<StripeSessionState> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await new SessionService(_client).GetAsync(sessionId, cancellationToken: cancellationToken);
        return new StripeSessionState(
            session.Id,
            IsPaid: session.PaymentStatus == "paid",
            IsExpired: session.Status == "expired",
            PaymentIntentId: session.PaymentIntentId);
    }

    public async Task RefundAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        await new RefundService(_client).CreateAsync(
            new RefundCreateOptions { PaymentIntent = paymentIntentId },
            cancellationToken: cancellationToken);
    }
}
