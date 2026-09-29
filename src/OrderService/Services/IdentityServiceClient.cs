using System.Net.Http.Headers;
using System.Text.Json;

namespace OrderService.Services;

public class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;

    public IdentityServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsCourierAsync(Guid userId, string token)
    {
        // Injeta o token recebido pelo OrderService no pedido que vai para o IdentityService
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _httpClient.GetAsync($"/api/users/{userId}");
        
        if (!response.IsSuccessStatusCode)
            return false;

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        
        // Verifica se a propriedade "role" existe e tem o valor "Estafeta"
        if (document.RootElement.TryGetProperty("role", out var roleElement))
        {
            return roleElement.GetString()?.Equals("Estafeta", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        return false;
    }
}