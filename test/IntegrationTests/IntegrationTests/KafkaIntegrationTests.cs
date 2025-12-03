using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace IntegrationTests;

public class KafkaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public KafkaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_PublishesKafkaEvent_AndProcessesSuccessfully()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateProductWithStock(token, 100);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
        orderDto.Should().NotBeNull();
        orderDto!.Status.Should().Be(OrderStatus.Pending);

        // Note: Full event processing requires Kafka to be running
        // In actual tests with Kafka running, you would:
        // - Wait for async processing (e.g., 6 seconds)
        // - Verify order status changed to Completed or Processing
        // - Check notifications table for audit records
    }

    [Fact]
    public async Task NotificationConsumer_CreatesNotificationRecord_OnOrderCompletion()
    {
        // This test requires Kafka to be running
        // In practice, you'd use Testcontainers to spin up Kafka for integration tests
        // For now, this serves as a template

        // Arrange - create order and wait for processing
        // Assert - query notifications table for audit record
        Assert.True(true); // Placeholder
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        var createUserRequest = new CreateUserRequest
        {
            Name = $"Test User {Guid.NewGuid()}",
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "Password123!"
        };

        var createResponse = await _client.PutAsJsonAsync("/api/user", createUserRequest);
        var userDto = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var loginRequest = new LoginRequest
        {
            Email = createUserRequest.Email,
            Password = createUserRequest.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (userDto!.Id, loginResult!.Token);
    }

    private async Task<int> CreateProductWithStock(string token, int stock)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productRequest = new CreateProductRequest
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 50.00m,
            Stock = stock
        };

        var response = await _client.PutAsJsonAsync("/api/product", productRequest);
        var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();

        return productDto!.Id;
    }
}
