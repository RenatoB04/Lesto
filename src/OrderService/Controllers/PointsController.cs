using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var points = await _context.Points
            .Select(p => new PointDto { Id = p.Id, Name = p.Name })
            .ToListAsync();

        return Ok(points);
    }
}