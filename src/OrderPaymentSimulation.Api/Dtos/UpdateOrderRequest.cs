using System.ComponentModel.DataAnnotations;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

public class UpdateOrderRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public OrderStatus Status { get; set; }
}
