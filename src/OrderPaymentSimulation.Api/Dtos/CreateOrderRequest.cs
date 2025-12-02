using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Items are required")]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
