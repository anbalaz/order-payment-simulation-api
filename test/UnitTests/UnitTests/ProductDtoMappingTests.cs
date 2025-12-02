using FluentAssertions;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class ProductDtoMappingTests
{
    [Fact]
    public void ProductDto_CreateFrom_MapsCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = ProductDto.CreateFrom(product);

        // Assert
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be(product.Name);
        dto.Description.Should().Be(product.Description);
        dto.Price.Should().Be(product.Price);
        dto.Stock.Should().Be(product.Stock);
        dto.CreatedAt.Should().Be(product.CreatedAt);
    }

    [Fact]
    public void ProductDto_CreateFrom_WithNullDescription_MapsCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = null,
            Price = 99.99m,
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = ProductDto.CreateFrom(product);

        // Assert
        dto.Description.Should().BeNull();
    }
}
