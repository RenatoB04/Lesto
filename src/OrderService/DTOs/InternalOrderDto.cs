namespace OrderService.DTOs;

public class InternalOrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
}
