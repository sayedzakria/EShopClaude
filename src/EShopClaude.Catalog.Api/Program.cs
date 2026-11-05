var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/", () => "Catalog API is running.");

app.MapDefaultEndpoints();

app.Run();
