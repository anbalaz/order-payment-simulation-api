using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace UnitTests;

public class JwtServiceTests
{
    private readonly Fixture _fixture;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        _fixture = new Fixture();

        // Setup configuration
        var configData = new Dictionary<string, string>
        {
            { "Jwt:Key", "TestSecretKeyThatIsAtLeast32CharactersLongForHS256" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpiryMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsToken()
    {
        // Arrange
        var service = new JwtService(_configuration);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = service.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var service = new JwtService(_configuration);
        var user = new User
        {
            Id = 42,
            Name = "John Doe",
            Email = "john@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == "nameid" && c.Value == "42");
        claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "John Doe");
        claims.Should().Contain(c => c.Type == "email" && c.Value == "john@example.com");
    }
}
