using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace IntegrationTests;

public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OrderControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsCreated()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product with stock
        var productId = await CreateProductWithStock(token, 100);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 2
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
        orderDto.Should().NotBeNull();
        orderDto!.Status.Should().Be(OrderStatus.Pending);
        orderDto.Items.Should().HaveCount(1);
        orderDto.Items[0].Quantity.Should().Be(2);
        orderDto.Total.Should().BeGreaterThan(0);

        // Verify stock decreased
        var productResponse = await _client.GetAsync($"/api/product/{productId}");
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        product!.Stock.Should().Be(98); // 100 - 2
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProductId_ReturnsUnprocessableEntity()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = 99999,  // Non-existent product
                    Quantity = 1
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ReturnsUnprocessableEntity()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product with limited stock
        var productId = await CreateProductWithStock(token, 5);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 10  // More than available stock
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task CreateOrder_WithQuantityZero_ReturnsBadRequest()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateProductWithStock(token, 100);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 0  // Invalid: must be > 0
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrder_OwnOrder_ReturnsOk()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an order
        var productId = await CreateProductWithStock(token, 100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };
        var createResponse = await _client.PutAsJsonAsync("/api/order", orderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Act
        var response = await _client.GetAsync($"/api/order/{createdOrder!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
        orderDto!.Id.Should().Be(createdOrder.Id);
    }

    [Fact]
    public async Task GetOrder_OtherUsersOrder_ReturnsForbidden()
    {
        // Arrange - User A creates an order
        var (_, tokenA) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var productId = await CreateProductWithStock(tokenA, 100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };
        var createResponse = await _client.PutAsJsonAsync("/api/order", orderRequest);
        var orderA = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // User B tries to access User A's order
        var (_, tokenB) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        // Act
        var response = await _client.GetAsync($"/api/order/{orderA!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrderStatus_OwnOrder_ReturnsOk()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an order
        var productId = await CreateProductWithStock(token, 100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };
        var createResponse = await _client.PutAsJsonAsync("/api/order", orderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Update status
        var updateRequest = new UpdateOrderRequest
        {
            Id = createdOrder!.Id,
            Status = OrderStatus.Processing
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/order/{createdOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedOrder = await response.Content.ReadFromJsonAsync<OrderDto>();
        updatedOrder!.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task DeleteOrder_PendingOrder_ReturnsOk()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a pending order
        var productId = await CreateProductWithStock(token, 100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };
        var createResponse = await _client.PutAsJsonAsync("/api/order", orderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/order/{createdOrder!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify order is deleted
        var getResponse = await _client.GetAsync($"/api/order/{createdOrder.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteOrder_ProcessingOrder_ReturnsConflict()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an order
        var productId = await CreateProductWithStock(token, 100);
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 }
            }
        };
        var createResponse = await _client.PutAsJsonAsync("/api/order", orderRequest);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Update status to Processing
        var updateRequest = new UpdateOrderRequest
        {
            Id = createdOrder!.Id,
            Status = OrderStatus.Processing
        };
        await _client.PostAsJsonAsync($"/api/order/{createdOrder.Id}", updateRequest);

        // Act - Try to delete
        var response = await _client.DeleteAsync($"/api/order/{createdOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Only pending orders can be deleted");
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOnlyCurrentUsersOrders()
    {
        // Arrange - User A creates 2 orders
        var (_, tokenA) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var productIdA1 = await CreateProductWithStock(tokenA, 100);
        var productIdA2 = await CreateProductWithStock(tokenA, 100);

        await _client.PutAsJsonAsync("/api/order", new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productIdA1, Quantity = 1 }
            }
        });
        await _client.PutAsJsonAsync("/api/order", new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productIdA2, Quantity = 1 }
            }
        });

        // User B creates 1 order
        var (_, tokenB) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var productIdB = await CreateProductWithStock(tokenB, 100);
        await _client.PutAsJsonAsync("/api/order", new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productIdB, Quantity = 1 }
            }
        });

        // Act - User A calls GetAll
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var response = await _client.GetAsync("/api/order");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        orders.Should().NotBeNull();
        orders!.Count.Should().Be(2); // Only User A's 2 orders
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        var email = $"user{Guid.NewGuid()}@example.com";

        var createRequest = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = "Password123!"
        };
        var createResponse = await _client.PutAsJsonAsync("/api/user", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (createdUser!.Id, loginResult!.Token);
    }

    private async Task<int> CreateProductWithStock(string token, int stock = 100)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = $"Product {Guid.NewGuid()}",
            Description = "Test",
            Price = 10.00m,
            Stock = stock
        };

        var response = await _client.PutAsJsonAsync("/api/product", createRequest);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        return product!.Id;
    }
}
