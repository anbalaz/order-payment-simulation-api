using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateOrderItemRequest
{
    [Required(ErrorMessage = "ProductId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a valid ID")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
