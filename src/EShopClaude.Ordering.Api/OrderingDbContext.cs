using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Ordering.Api;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.ToTable("Order");
            order.HasKey(o => o.Id);
            order.Property(o => o.Total).HasColumnType("numeric(18,2)");
            order.Property(o => o.Status).IsRequired().HasMaxLength(20);
            order.Property(o => o.CreatedDate).IsRequired();
            order.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.ToTable("OrderItem");
            item.HasKey(i => i.Id);
            item.Property(i => i.ProductId).IsRequired();
            item.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            item.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
            item.Property(i => i.Quantity).IsRequired();
        });
    }
}
