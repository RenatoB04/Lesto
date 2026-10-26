using System.Net;
using System.Net.Http.Json;
using PaymentService.DTOs;

namespace PaymentService.Services;

public class OrderServiceClient : IOrderServiceClient
{
    private const string InternalKeyHeader = "X-Internal-Key";
    private readonly HttpClient _httpClient;
    private readonly string _internalKey;

    public OrderServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _internalKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
            ?? throw new InvalidOperationException("INTERNAL_API_KEY não está definida.");
    }

    public async Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        // Enviar a chave interna em cada pedido
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/internal/orders/{orderId}");
        request.Headers.Add(InternalKeyHeader, _internalKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OrderSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, string paymentStatus, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/internal/orders/{orderId}/payment")
        {
            Content = JsonContent.Create(new { paymentStatus })
        };
        request.Headers.Add(InternalKeyHeader, _internalKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
