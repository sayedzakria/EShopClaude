var builder = DistributedApplication.CreateBuilder(args);

var catalogApi = builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalogapi")
    .WithHttpHealthCheck("/health");

var basketApi = builder.AddProject<Projects.EShopClaude_Basket_Api>("basketapi")
    .WithHttpHealthCheck("/health");

var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("orderingapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
