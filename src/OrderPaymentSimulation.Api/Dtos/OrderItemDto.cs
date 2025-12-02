using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for OrderItem response (includes product details)
/// </summary>
public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static OrderItemDto CreateFrom(OrderItem item)
        => new()
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            Quantity = item.Quantity,
            Price = item.Price,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
}
