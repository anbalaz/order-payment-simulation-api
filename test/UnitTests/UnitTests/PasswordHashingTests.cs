using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class PasswordHashingTests
{
    [Fact]
    public void HashPassword_ProducesHashedString()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";

        // Act
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(plainPassword);
    }

    [Fact]
    public void VerifyHashedPassword_WithCorrectPassword_Succeeds()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Act
        var result = hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_Fails()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Act
        var result = hasher.VerifyHashedPassword(user, hashedPassword, wrongPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.Failed);
    }
}
