using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IOsrmService _osrmService;

    public OrdersController(OrderDbContext context, IOsrmService osrmService)
    {
        _context = context;
        _osrmService = osrmService;
    }

    [HttpPost]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // 1. Validar o Peso Máximo
        var settingPeso = await _context.Settings.FindAsync("PesoMaximoKg");
        if (settingPeso != null && double.TryParse(settingPeso.Value, out double pesoMaximo))
        {
            if (dto.Weight > pesoMaximo)
            {
                return BadRequest(new { Message = $"O peso excede o limite máximo permitido de {pesoMaximo}kg." });
            }
        }

        // 2. Obter as coordenadas de Origem e Destino
        var origem = await _context.Points.FindAsync(dto.OriginPointId);
        var destino = await _context.Points.FindAsync(dto.DestinationPointId);

        if (origem == null || destino == null)
        {
            return BadRequest(new { Message = "Ponto de origem ou destino inválido." });
        }

        // 3. Obter Distância e Duração reais através do OSRM
        double distancia = 0;
        double duracao = 0;
        try
        {
            var rota = await _osrmService.ObterRotaAsync(origem.Latitude, origem.Longitude, destino.Latitude, destino.Longitude);
            distancia = rota.DistanciaKm;
            duracao = rota.DuracaoMin;
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { Message = "Erro ao calcular a rota. Tente mais tarde.", Detalhe = ex.Message });
        }

        // 4. Obter o ID do utilizador autenticado a partir do Token JWT
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid customerId))
        {
            return Unauthorized();
        }

        // 5. Criar e guardar a encomenda
        var order = new Order
        {
            CustomerId = customerId,
            OriginPointId = origem.Id,
            DestinationPointId = destino.Id,
            Weight = dto.Weight,
            Type = dto.Type,
            RecipientData = dto.RecipientData,
            Distance = distancia,
            Duration = duracao,
            Status = OrderStatus.Pending
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { Message = "Encomenda criada com sucesso.", OrderId = order.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid customerId)) return Unauthorized();

        var orders = await _context.Orders
            .Include(o => o.Origin)
            .Include(o => o.Destination)
            .Where(o => o.CustomerId == customerId)
            .Select(o => MapearParaDto(o))
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Cliente")]
    public async Task<IActionResult> GetMyOrderById(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid customerId)) return Unauthorized();

        var order = await _context.Orders
            .Include(o => o.Origin)
            .Include(o => o.Destination)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        return Ok(MapearParaDto(order));
    }

    // Método auxiliar para evitar duplicação de código
    private static OrderDto MapearParaDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            Weight = order.Weight,
            Type = order.Type,
            RecipientData = order.RecipientData,
            Distance = order.Distance,
            Duration = order.Duration,
            Origin = new PointDto { Id = order.Origin.Id, Name = order.Origin.Name },
            Destination = new PointDto { Id = order.Destination.Id, Name = order.Destination.Name }
        };
    }
}