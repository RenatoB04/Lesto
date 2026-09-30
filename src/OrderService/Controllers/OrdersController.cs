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
    private readonly IIdentityServiceClient _identityServiceClient;

    public OrdersController(OrderDbContext context, IOsrmService osrmService, IIdentityServiceClient identityServiceClient)
    {
        _context = context;
        _osrmService = osrmService;
        _identityServiceClient = identityServiceClient;
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
    [Authorize] // Qualquer utilizador autenticado pode aceder, a lógica filtra os dados
    public async Task<IActionResult> GetOrders([FromQuery] string? estado)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Unauthorized();

        IQueryable<Order> query = _context.Orders.Include(o => o.Origin).Include(o => o.Destination);

        if (User.IsInRole("Cliente"))
        {
            // Clientes só veem as suas
            query = query.Where(o => o.CustomerId == userId);
        }
        else if (User.IsInRole("Gestor") || User.IsInRole("Administrador"))
        {
            // Gestores veem todas, com filtro opcional por estado
            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<OrderStatus>(estado, true, out var statusEnum))
            {
                query = query.Where(o => o.Status == statusEnum);
            }
        }
        else if (User.IsInRole("Estafeta"))
        {
            // Estafetas SÓ veem as que lhes estão atribuídas
            query = query.Where(o => o.CourierId == userId);
            
            // Também podem filtrar pelo estado (ex: ver apenas as "Validated" que têm de ir recolher)
            if (!string.IsNullOrEmpty(estado) && Enum.TryParse<OrderStatus>(estado, true, out var statusEnum))
            {
                query = query.Where(o => o.Status == statusEnum);
            }
        }
        else
        {
            return Forbid();
        }

        var orders = await query.Select(o => MapearParaDto(o)).ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Origin)
            .Include(o => o.Destination)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        // Se for Cliente, garantir que a encomenda lhe pertence
        if (User.IsInRole("Cliente"))
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out Guid customerId) || order.CustomerId != customerId)
            {
                return Forbid();
            }
        }

        return Ok(MapearParaDto(order));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Gestor, Administrador")]
    public async Task<IActionResult> RejectOrder(Guid id, [FromBody] RejectOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { Message = "Apenas encomendas pendentes (Pending) podem ser rejeitadas." });

        order.Status = OrderStatus.Rejected;
        order.Reason = dto.Reason;
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Encomenda rejeitada com sucesso." });
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Gestor, Administrador")]
    public async Task<IActionResult> AssignOrder(Guid id, [FromBody] AssignOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { Message = "Apenas encomendas pendentes podem ser atribuídas." });

        // Extrair o token do cabeçalho atual para o passar ao Identity Service
        var authHeader = Request.Headers["Authorization"].ToString();
        var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring(7) : authHeader;

        // Chamar o Identity Service
        var isCourier = await _identityServiceClient.IsCourierAsync(dto.CourierId, token);
        if (!isCourier)
            return BadRequest(new { Message = "O utilizador selecionado não existe ou não tem o perfil de Estafeta." });

        order.CourierId = dto.CourierId;
        order.Status = OrderStatus.Validated; 
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Encomenda atribuída com sucesso ao Estafeta." });
    }

    [HttpPost("{id}/pickup")]
    [Authorize(Roles = "Estafeta")]
    public async Task<IActionResult> PickupOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid courierId) || order.CourierId != courierId)
        {
            return Forbid(); // Garante que não mexe em encomendas de outros estafetas
        }

        if (order.Status != OrderStatus.Validated)
            return Conflict(new { Message = "Apenas encomendas validadas podem ser recolhidas." });

        order.Status = OrderStatus.InTransit;
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Encomenda recolhida e em trânsito." });
    }

    [HttpPost("{id}/deliver")]
    [Authorize(Roles = "Estafeta")]
    public async Task<IActionResult> DeliverOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid courierId) || order.CourierId != courierId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.InTransit)
            return Conflict(new { Message = "Apenas encomendas em trânsito podem ser alteradas." });

        order.Status = OrderStatus.Delivered;
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Encomenda entregue com sucesso." });
    }

    [HttpPost("{id}/fail")]
    [Authorize(Roles = "Estafeta")]
    public async Task<IActionResult> FailOrder(Guid id, [FromBody] FailOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid courierId) || order.CourierId != courierId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.InTransit)
            return Conflict(new { Message = "Apenas encomendas em trânsito podem ser alteradas." });

        order.Status = OrderStatus.Failed;
        order.Reason = dto.Reason; 
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Falha na entrega registada." });
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