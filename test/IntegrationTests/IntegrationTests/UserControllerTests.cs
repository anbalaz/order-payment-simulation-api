using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using Microsoft.AspNetCore.Identity;

namespace IntegrationTests;

public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/user", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = $"user{Guid.NewGuid()}@example.com";

        // Create first user
        var firstUser = new CreateUserRequest
        {
            Name = "First User",
            Email = email,
            Password = "Password123!"
        };
        await _client.PutAsJsonAsync("/api/user", firstUser);

        // Try to create duplicate
        var createRequest = new CreateUserRequest
        {
            Name = "Duplicate User",
            Email = email, // Already exists
            Password = "Password123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/user", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUser_WithAuthentication_ReturnsUser()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/user/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateUserRequest
        {
            Id = userId,
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/user", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteUser_WithAuthentication_ReturnsOk()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();
        var user = await context.Users.FindAsync(userId);
        user.Should().BeNull();
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        var email = $"user{Guid.NewGuid()}@example.com";

        // Create user
        var createRequest = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = "Password123!"
        };
        var createResponse = await _client.PutAsJsonAsync("/api/user", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Login to get token
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (createdUser!.Id, loginResult!.Token);
    }
}
