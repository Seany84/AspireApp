using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithDbGate();


var daprState = builder.AddDaprStateStore("statestore");
var daprPubSub = builder.AddDaprPubSub("pubsub");

var apiService = builder.AddProject<Projects.AspireApps_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithDaprSidecar()
    .WithReference(daprState)
    .WithReference(daprPubSub);

builder.AddProject<Projects.AspireApps_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithDaprSidecar()
    .WithReference(daprState)
    .WithReference(daprPubSub)
    .WithReference(apiService)
    .WaitFor(apiService);

//Inject Dapr client for DI / service-to-service calls
builder.Services.AddDaprClient();

builder.Build().Run();