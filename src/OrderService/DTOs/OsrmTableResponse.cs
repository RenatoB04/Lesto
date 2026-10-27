using System.Text.Json.Serialization;

namespace OrderService.DTOs;

public class OsrmTableResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    // Matriz em metros (null quando não há caminho entre dois pontos)
    [JsonPropertyName("distances")]
    public List<List<double?>> Distances { get; set; } = new();

    // Matriz em segundos
    [JsonPropertyName("durations")]
    public List<List<double?>> Durations { get; set; } = new();
}
