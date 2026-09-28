namespace OrderService.Models;

public enum OrderStatus
{
    Pending,
    Validated,
    Rejected,
    InTransit,
    Delivered,
    Failed
}