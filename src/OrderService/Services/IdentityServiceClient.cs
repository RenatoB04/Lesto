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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        
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