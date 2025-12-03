namespace order_payment_simulation_api.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
