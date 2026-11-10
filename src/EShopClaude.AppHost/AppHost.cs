var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();
var catalogdb = postgres.AddDatabase("catalogdb");
var orderingdb = postgres.AddDatabase("orderingdb");

var redis = builder.AddRedis("redis");

var catalogApi = builder.AddProject<Projects.EShopClaude_Catalog_Api>("catalogapi")
    .WithReference(catalogdb)
    .WaitFor(catalogdb)
    .WithHttpHealthCheck("/health");

var basketApi = builder.AddProject<Projects.EShopClaude_Basket_Api>("basketapi")
    .WithHttpHealthCheck("/health");

var orderingApi = builder.AddProject<Projects.EShopClaude_Ordering_Api>("orderingapi")
    .WithReference(orderingdb)
    .WaitFor(orderingdb)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.EShopClaude_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();
