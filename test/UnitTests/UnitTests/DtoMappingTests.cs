using FluentAssertions;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class DtoMappingTests
{
    [Fact]
    public void UserDto_CreateFrom_MapsCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = UserDto.CreateFrom(user);

        // Assert
        dto.Id.Should().Be(user.Id);
        dto.Name.Should().Be(user.Name);
        dto.Email.Should().Be(user.Email);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public void UserDto_DoesNotIncludePassword()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = UserDto.CreateFrom(user);

        // Assert
        var dtoType = typeof(UserDto);
        dtoType.GetProperty("Password").Should().BeNull();
    }
}
