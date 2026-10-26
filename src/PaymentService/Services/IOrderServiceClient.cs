using PaymentService.DTOs;

namespace PaymentService.Services;

public interface IOrderServiceClient
{
    // Devolve null se a encomenda não existir
    Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    // Devolve true se o Order Service aceitou a alteração
    Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, CancellationToken cancellationToken = default);
}
