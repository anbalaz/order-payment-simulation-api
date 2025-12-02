using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).HasColumnName("id");

        builder.Property(oi => oi.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("idx_order_items_order_id");

        builder.Property(oi => oi.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("idx_order_items_product_id");

        builder.Property(oi => oi.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(oi => oi.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(oi => oi.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(oi => oi.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
