using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;

namespace IntegrationTests;

public class ProductControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/product", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();
        productDto.Should().NotBeNull();
        productDto!.Name.Should().Be("Test Product");
        productDto.Stock.Should().Be(100);
        productDto.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = -1m,  // Invalid: negative price
            Stock = 100
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/product", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNegativeStock_ReturnsBadRequest()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = -1  // Invalid: negative stock
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/product", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProduct_WithAuthentication_ReturnsProduct()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };
        var createResponse = await _client.PutAsJsonAsync("/api/product", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Act
        var response = await _client.GetAsync($"/api/product/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();
        productDto.Should().NotBeNull();
        productDto!.Id.Should().Be(createdProduct.Id);
        productDto.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/product/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsOk()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product first
        var createRequest = new CreateProductRequest
        {
            Name = "Original Product",
            Description = "Original Description",
            Price = 99.99m,
            Stock = 100
        };
        var createResponse = await _client.PutAsJsonAsync("/api/product", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Update the product
        var updateRequest = new UpdateProductRequest
        {
            Id = createdProduct!.Id,
            Name = "Updated Product",
            Description = "Updated Description",
            Price = 89.99m,
            Stock = 90
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/product/{createdProduct.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDto = await response.Content.ReadFromJsonAsync<ProductDto>();
        updatedDto!.Name.Should().Be("Updated Product");
        updatedDto.Price.Should().Be(89.99m);
        updatedDto.Stock.Should().Be(90);
    }

    [Fact]
    public async Task GetAllProducts_WithAuthentication_ReturnsProducts()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create two products
        await _client.PutAsJsonAsync("/api/product", new CreateProductRequest
        {
            Name = "Product 1",
            Description = "Description 1",
            Price = 10.00m,
            Stock = 10
        });
        await _client.PutAsJsonAsync("/api/product", new CreateProductRequest
        {
            Name = "Product 2",
            Description = "Description 2",
            Price = 20.00m,
            Stock = 20
        });

        // Act
        var response = await _client.GetAsync("/api/product");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        products.Should().NotBeNull();
        products!.Count.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteProduct_NotReferencedInOrders_ReturnsOk()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product
        var createRequest = new CreateProductRequest
        {
            Name = "Product to Delete",
            Description = "Will be deleted",
            Price = 50.00m,
            Stock = 50
        };
        var createResponse = await _client.PutAsJsonAsync("/api/product", createRequest);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/product/{createdProduct!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify product is deleted
        var getResponse = await _client.GetAsync($"/api/product/{createdProduct.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_ReferencedInOrders_ReturnsConflict()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a product
        var productRequest = new CreateProductRequest
        {
            Name = "Product in Order",
            Description = "Cannot be deleted",
            Price = 100.00m,
            Stock = 100
        };
        var productResponse = await _client.PutAsJsonAsync("/api/product", productRequest);
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Create an order with this product
        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = product!.Id,
                    Quantity = 1
                }
            }
        };
        await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Act - Try to delete the product
        var response = await _client.DeleteAsync($"/api/product/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
}
