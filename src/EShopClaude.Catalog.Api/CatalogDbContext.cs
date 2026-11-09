using Microsoft.EntityFrameworkCore;

namespace EShopClaude.Catalog.Api;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("Product");
            product.HasKey(p => p.Id);
            product.Property(p => p.Name).IsRequired().HasMaxLength(200);
            product.Property(p => p.Description).IsRequired().HasMaxLength(1000);
            product.Property(p => p.Price).HasColumnType("numeric(18,2)");
            product.Property(p => p.Stock).IsRequired();
        });
    }
}
