using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Cliente")]
public class GeocodeController : ControllerBase
{
    public const int MaxAddressLength = 200;

    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GeocodeController> _logger;

    public GeocodeController(IGeocodingService geocodingService, ILogger<GeocodeController> logger)
    {
        _geocodingService = geocodingService;
        _logger = logger;
    }

    // Pré-visualizar uma morada antes de criar a encomenda (o cliente confirma o local encontrado)
    [HttpGet]
    public async Task<IActionResult> Geocode([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var address = NormalizarMorada(q);
        if (address == null)
            return BadRequest(new { Message = $"Indique uma morada com até {MaxAddressLength} caracteres." });

        GeocodingResult? result;
        try
        {
            result = await _geocodingService.GeocodeAsync(address, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to geocode an address.");
            return StatusCode(503, new { Message = "Serviço de moradas indisponível. Tente mais tarde." });
        }

        if (result == null)
            return NotFound(new { Message = "Morada não encontrada." });

        return Ok(new GeocodeResultDto
        {
            Query = address,
            DisplayName = result.DisplayName,
            Latitude = result.Latitude,
            Longitude = result.Longitude
        });
    }

    // Método auxiliar para limpar a morada (espaços a mais); devolve null se for inválida
    public static string? NormalizarMorada(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var normalized = string.Join(' ', address.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return normalized.Length > MaxAddressLength ? null : normalized;
    }
}
