using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PointsController : ControllerBase
{
    private readonly OrderDbContext _context;

    public PointsController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPoints()
    {
        // Só os pontos do catálogo (as moradas dos clientes não são públicas)
        var points = await _context.Points
            .Where(p => p.IsCatalog)
            .Select(p => new PointDto { Id = p.Id, Name = p.Name })
            .ToListAsync();

        return Ok(points);
    }
}