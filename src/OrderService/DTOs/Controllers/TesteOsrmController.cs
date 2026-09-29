using Microsoft.AspNetCore.Mvc;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TesteOsrmController : ControllerBase
{
    private readonly IOsrmService _osrmService;

    public TesteOsrmController(IOsrmService osrmService)
    {
        _osrmService = osrmService;
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
            return BadRequest(new { Erro = ex.Message });
        }
    }
}