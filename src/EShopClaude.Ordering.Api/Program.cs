var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/", () => "Ordering API is running.");

app.MapDefaultEndpoints();

app.Run();
