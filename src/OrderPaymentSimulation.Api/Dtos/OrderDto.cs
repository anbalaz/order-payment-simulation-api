using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for Order response (includes nested order items)
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();

    public static OrderDto CreateFrom(Order order)
        => new()
        {
            Id = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems?.Select(OrderItemDto.CreateFrom).ToList() ?? new()
        };
}
