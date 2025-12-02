using FluentAssertions;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class OrderDtoMappingTests
{
    [Fact]
    public void OrderDto_CreateFrom_MapsCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 10.00m,
            Stock = 100
        };

        var orderItem = new OrderItem
        {
            Id = 1,
            ProductId = 1,
            Product = product,
            Quantity = 2,
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Total = 20.00m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem> { orderItem }
        };

        // Act
        var dto = OrderDto.CreateFrom(order);

        // Assert
        dto.Id.Should().Be(order.Id);
        dto.UserId.Should().Be(order.UserId);
        dto.Total.Should().Be(order.Total);
        dto.Status.Should().Be(order.Status);
        dto.CreatedAt.Should().Be(order.CreatedAt);
        dto.UpdatedAt.Should().Be(order.UpdatedAt);
        dto.Items.Should().HaveCount(1);
    }

    [Fact]
    public void OrderItemDto_CreateFrom_IncludesProductName()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Price = 10.00m,
            Stock = 100
        };

        var orderItem = new OrderItem
        {
            Id = 1,
            ProductId = 1,
            Product = product,
            Quantity = 2,
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = OrderItemDto.CreateFrom(orderItem);

        // Assert
        dto.Id.Should().Be(orderItem.Id);
        dto.ProductId.Should().Be(orderItem.ProductId);
        dto.ProductName.Should().Be("Test Product");
        dto.Quantity.Should().Be(orderItem.Quantity);
        dto.Price.Should().Be(orderItem.Price);
        dto.CreatedAt.Should().Be(orderItem.CreatedAt);
        dto.UpdatedAt.Should().Be(orderItem.UpdatedAt);
    }

    [Fact]
    public void OrderItemDto_CreateFrom_WithNullProduct_HandlesGracefully()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Id = 1,
            ProductId = 1,
            Product = null,  // Product not loaded
            Quantity = 2,
            Price = 10.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = OrderItemDto.CreateFrom(orderItem);

        // Assert
        dto.ProductName.Should().Be(string.Empty);
    }
}
