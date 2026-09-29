using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Qualquer utilizador autenticado pode aceder por defeito
public class SettingsController : ControllerBase
{
    private readonly OrderDbContext _context;

    public SettingsController(OrderDbContext context)
    {
        _context = context;
    }

    // Obter todas as definições (Útil para a App saber as regras atuais)
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.Settings
            .Select(s => new SettingDto { Key = s.Key, Value = s.Value })
            .ToListAsync();
        
        return Ok(settings);
    }

    // Atualizar uma definição (Apenas o Administrador)
    [HttpPut("{key}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
    {
        var setting = await _context.Settings.FindAsync(key);
        if (setting == null)
        {
            return NotFound(new { Message = $"A definição '{key}' não foi encontrada." });
        }

        // Validação adicional: garantir que se for o peso, é um número válido
        if (key == "PesoMaximoKg" && !double.TryParse(dto.Value, out _))
        {
            return BadRequest(new { Message = "O valor para PesoMaximoKg tem de ser um número válido." });
        }

        setting.Value = dto.Value;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Definição atualizada com sucesso." });
    }
}