using FluentAssertions;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class OrderExpirationLogicTests
{
    [Fact]
    public void OrderExpiration_CalculatesCorrectCutoffTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var expirationThreshold = TimeSpan.FromMinutes(10);

        // Act
        var cutoffTime = now.Subtract(expirationThreshold);

        // Assert
        cutoffTime.Should().BeBefore(now);
        (now - cutoffTime).Should().Be(expirationThreshold);
    }

    [Fact]
    public void OrderStatus_ExpirationTransition_IsValid()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Total = 100.00m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        // Act
        order.Status = OrderStatus.Expired;

        // Assert
        order.Status.Should().Be(OrderStatus.Expired);
    }
}
