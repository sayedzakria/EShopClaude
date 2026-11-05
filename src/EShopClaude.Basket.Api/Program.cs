var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/", () => "Basket API is running.");

app.MapDefaultEndpoints();

app.Run();
