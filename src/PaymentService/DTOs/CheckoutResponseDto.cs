namespace PaymentService.DTOs;

public class CheckoutResponseDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "eur";
    public string CheckoutUrl { get; set; } = string.Empty;
}
