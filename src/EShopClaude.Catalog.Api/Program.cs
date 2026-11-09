using EShopClaude.Catalog.Api;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreated();
}

app.MapGet("/", () => "Catalog API is running.");

app.MapDefaultEndpoints();

app.Run();
