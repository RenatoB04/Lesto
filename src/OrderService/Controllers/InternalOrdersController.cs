using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Security;

namespace OrderService.Controllers;

[ApiController]
[Route("internal/orders")]
[AllowAnonymous]
[InternalApiKey]
[ApiExplorerSettings(IgnoreApi = true)]
public class InternalOrdersController : ControllerBase
{
    private readonly OrderDbContext _context;

    public InternalOrdersController(OrderDbContext context)
    {
        _context = context;
    }

    // Obter o resumo da encomenda (usado pelo Payment Service antes de criar o pagamento)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        return Ok(new InternalOrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Price = order.Price,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString()
        });
    }

    // Atualizar o estado do pagamento (Paid ou Refunded)
    [HttpPost("{id}/payment")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, [FromBody] UpdatePaymentStatusDto dto, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentStatus>(dto.PaymentStatus, true, out var newStatus) || newStatus == PaymentStatus.Unpaid)
            return BadRequest(new { Message = "Estado de pagamento inválido." });

        var order = await _context.Orders.FindAsync(new object[] { id }, cancellationToken);
        if (order == null) return NotFound(new { Message = "Encomenda não encontrada." });

        // Só são permitidas as transições Unpaid -> Paid e Paid -> Refunded
        // Se o estado já for o pedido, não faz nada (o Payment Service pode repetir o pedido)
        if (order.PaymentStatus == newStatus)
            return Ok(new { Message = "Estado de pagamento já atualizado." });

        var valid = (order.PaymentStatus == PaymentStatus.Unpaid && newStatus == PaymentStatus.Paid)
                 || (order.PaymentStatus == PaymentStatus.Paid && newStatus == PaymentStatus.Refunded);
        if (!valid)
            return Conflict(new { Message = $"Não é possível passar de {order.PaymentStatus} para {newStatus}." });

        order.PaymentStatus = newStatus;
        if (newStatus == PaymentStatus.Paid) order.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = "Estado de pagamento atualizado." });
    }
}
