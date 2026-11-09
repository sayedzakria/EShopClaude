namespace EShopClaude.Catalog.Api;

public static class CatalogSeeder
{
    public static void Seed(CatalogDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        db.Products.AddRange(
            new Product
            {
                Name = "Wireless Headphones",
                Description = "Over-ear Bluetooth headphones with active noise cancelling and 30 hours of battery life.",
                Price = 129.99m,
                Stock = 40
            },
            new Product
            {
                Name = "Mechanical Keyboard",
                Description = "Compact 75% mechanical keyboard with hot-swappable switches and white backlighting.",
                Price = 89.50m,
                Stock = 25
            },
            new Product
            {
                Name = "Ergonomic Mouse",
                Description = "Vertical wireless mouse that keeps the wrist in a natural position, with adjustable DPI.",
                Price = 39.90m,
                Stock = 60
            },
            new Product
            {
                Name = "27-inch 4K Monitor",
                Description = "27-inch IPS monitor with 4K resolution, 60 Hz refresh rate and USB-C power delivery.",
                Price = 349.00m,
                Stock = 15
            },
            new Product
            {
                Name = "Full HD Webcam",
                Description = "1080p webcam with autofocus, a built-in microphone and a privacy shutter.",
                Price = 59.99m,
                Stock = 35
            },
            new Product
            {
                Name = "7-Port USB Hub",
                Description = "Powered USB 3.0 hub with seven ports and individual on/off switches.",
                Price = 24.95m,
                Stock = 80
            },
            new Product
            {
                Name = "Laptop Backpack",
                Description = "Water-resistant 20-litre backpack with a padded compartment for laptops up to 16 inches.",
                Price = 54.00m,
                Stock = 50
            },
            new Product
            {
                Name = "LED Desk Lamp",
                Description = "Dimmable LED desk lamp with five colour temperatures and a USB charging port.",
                Price = 32.75m,
                Stock = 45
            });

        db.SaveChanges();
    }
}
