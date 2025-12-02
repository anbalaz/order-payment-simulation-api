namespace order_payment_simulation_api.Models;

public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3,
    Expired = 4
}
