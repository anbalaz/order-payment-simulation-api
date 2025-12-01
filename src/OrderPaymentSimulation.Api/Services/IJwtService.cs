using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
