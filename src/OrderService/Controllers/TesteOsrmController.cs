using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class TesteOsrmController : ControllerBase
{
    private readonly IOsrmService _osrmService;
    private readonly ILogger<TesteOsrmController> _logger;

    public TesteOsrmController(IOsrmService osrmService, ILogger<TesteOsrmController> logger)
    {
        _osrmService = osrmService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> TestarRota()
    {
        // Vamos usar duas coordenadas reais de Braga para testar
        // Universidade do Minho -> Centro Histórico
        double latOrigem = 41.5614;
        double lonOrigem = -8.3973;
        double latDestino = 41.5503;
        double lonDestino = -8.4200;

        try
        {
            var resultado = await _osrmService.ObterRotaAsync(latOrigem, lonOrigem, latDestino, lonDestino);
            return Ok(new 
            { 
                Mensagem = "Ligação ao OSRM com sucesso!",
                DistanciaEmKm = resultado.DistanciaKm, 
                DuracaoEmMinutos = resultado.DuracaoMin 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test the OSRM route service.");
            return BadRequest(new { Erro = "Não foi possível testar a ligação ao serviço de rotas." });
        }
    }
}