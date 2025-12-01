using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Data.Configurations;

namespace order_payment_simulation_api.Data;

public class OrderPaymentDbContext : DbContext
{
    public OrderPaymentDbContext(DbContextOptions<OrderPaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
    }
}
