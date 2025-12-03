namespace order_payment_simulation_api.Dtos;

public class OrderExpiredEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiredAt { get; set; }
}
