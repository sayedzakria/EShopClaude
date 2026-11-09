using EShopClaude.Catalog.Api;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.EnsureCreated();
    CatalogSeeder.Seed(db);
}

app.MapGet("/", () => "Catalog API is running.");

app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

app.MapGet("/products/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
        is { } product ? Results.Ok(product) : Results.NotFound());

app.MapDefaultEndpoints();

app.Run();
