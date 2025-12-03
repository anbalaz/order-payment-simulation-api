using Microsoft.AspNetCore.Identity;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data;

public static class SeedData
{
    public static void Initialize(OrderPaymentDbContext context)
    {
        // Check if already seeded
        if (context.Users.Any())
        {
            return; // Database has been seeded
        }

        var passwordHasher = new PasswordHasher<User>();

        // Seed Users
        var users = new[]
        {
            new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Hash passwords
        foreach (var user in users)
        {
            user.Password = passwordHasher.HashPassword(user, "Password123!");
        }

        context.Users.AddRange(users);
        context.SaveChanges();

        // Seed Products
        var products = new[]
        {
            new Product { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, Stock = 50, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, Stock = 200, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, Stock = 150, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Monitor", Description = "27-inch 4K monitor", Price = 399.99m, Stock = 75, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Headphones", Description = "Noise-cancelling headphones", Price = 199.99m, Stock = 100, CreatedAt = DateTime.UtcNow }
        };

        context.Products.AddRange(products);
        context.SaveChanges();

        // Seed Orders
        var testUser = users[1]; // Test user
        var order1 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Completed,
            Total = 1029.98m,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var order2 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Processing,
            Total = 479.98m,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var order3 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Pending,
            Total = 199.99m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Orders.AddRange(order1, order2, order3);
        context.SaveChanges();

        // Seed Order Items
        var orderItems = new[]
        {
            // Order 1 items
            new OrderItem { OrderId = order1.Id, ProductId = products[0].Id, Quantity = 1, Price = 999.99m, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-4) },
            new OrderItem { OrderId = order1.Id, ProductId = products[1].Id, Quantity = 1, Price = 29.99m, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-4) },

            // Order 2 items
            new OrderItem { OrderId = order2.Id, ProductId = products[3].Id, Quantity = 1, Price = 399.99m, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new OrderItem { OrderId = order2.Id, ProductId = products[2].Id, Quantity = 1, Price = 79.99m, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-1) },

            // Order 3 items
            new OrderItem { OrderId = order3.Id, ProductId = products[4].Id, Quantity = 1, Price = 199.99m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        context.OrderItems.AddRange(orderItems);
        context.SaveChanges();

        // Seed Notifications (sample audit trail)
        var notifications = new[]
        {
            new Notification
            {
                UserId = testUser.Id,
                OrderId = order1.Id,
                Message = $"Order #{order1.Id} for user {testUser.Email} was completed. Items: Laptop, Mouse. Total: ${order1.Total}",
                Status = NotificationStatus.OrderCompleted,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-4)
            }
        };

        context.Notifications.AddRange(notifications);
        context.SaveChanges();
    }
}
