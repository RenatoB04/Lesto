using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class SettingsController : ControllerBase
{
    private readonly OrderDbContext _context;

    public SettingsController(OrderDbContext context)
    {
        _context = context;
    }

    // Obter todas as definições (Útil para a App saber as regras atuais)
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _context.Settings
            .Select(s => new SettingDto { Key = s.Key, Value = s.Value })
            .ToListAsync(cancellationToken);
        
        return Ok(settings);
    }

    // Atualizar uma definição (Apenas o Administrador)
    [HttpPut("{key}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto, CancellationToken cancellationToken)
    {
        var setting = await _context.Settings.FindAsync(new object[] { key }, cancellationToken);
        if (setting == null)
        {
            return NotFound(new { Message = $"A definição '{key}' não foi encontrada." });
        }

        // Validação adicional: garantir que se for o peso, é um número válido
        if (key == "PesoMaximoKg" &&
            (!double.TryParse(dto.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var weight) || weight <= 0))
        {
            return BadRequest(new { Message = "O valor para PesoMaximoKg tem de ser um número válido." });
        }

        // Validação adicional: as taxas de preço têm de ser números maiores ou iguais a zero
        if ((key == "PrecoBaseEur" || key == "PrecoPorKmEur" || key == "PrecoPorKgEur") &&
            (!decimal.TryParse(dto.Value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price < 0))
        {
            return BadRequest(new { Message = $"O valor para {key} tem de ser um número maior ou igual a zero (ex.: 0.40)." });
        }

        setting.Value = dto.Value;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { Message = "Definição atualizada com sucesso." });
    }
}