using System.Text.Json.Serialization;

namespace OrderService.DTOs;

public class OsrmResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("routes")]
    public List<OsrmRoute> Routes { get; set; } = new();
}

public class OsrmRoute
{
    // A distância vem em metros
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    // A duração vem em segundos
    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}