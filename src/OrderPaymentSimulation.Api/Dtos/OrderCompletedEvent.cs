namespace order_payment_simulation_api.Dtos;

public class OrderCompletedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}
