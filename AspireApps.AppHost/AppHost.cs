using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithDbGate();

var daprState = builder.AddDaprStateStore("statestore");

DaprSidecarOptions sideCarOptions = new DaprSidecarOptions
{
    EnableAppHealthCheck = true,
    AppHealthCheckPath = "/health",
};

IResourceBuilder<RabbitMQServerResource>? rabbitmq = null;
var pubSubType = "pubsub.azure.servicebus";

if (builder.Environment.IsDevelopment())
{
    var username = builder.AddParameterFromConfiguration("rabbitmq-username", "guest", true);
    var password = builder.AddParameterFromConfiguration("rabbitmq-password", "guest", true);
    rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password)
        .WithManagementPlugin();

    pubSubType = "pubsub.rabbitmq";
}
var daprPubSubBuilder = builder.AddDaprComponent("pubsub", pubSubType, new DaprComponentOptions
{
    LocalPath = Path.Combine("..", "dapr", "components")
});

var apiService = builder.AddProject<Projects.AspireApps_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithDaprSidecar(sideCarOptions)
    .WithReference(daprState)
    .WithReference(daprPubSubBuilder);

// Wait for RabbitMQ in development
if (rabbitmq != null)
{
    apiService = apiService.WithReference(rabbitmq).WaitFor(rabbitmq);
}

var webFrontend = builder.AddProject<Projects.AspireApps_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithDaprSidecar(sideCarOptions)
    .WithReference(daprState)
    .WithReference(daprPubSubBuilder)
    .WithReference(apiService)
    .WaitFor(apiService);

// Wait for RabbitMQ in development
if (rabbitmq != null)
{
    webFrontend = webFrontend.WithReference(rabbitmq).WaitFor(rabbitmq);
}

//Inject Dapr client for DI / service-to-service calls
builder.Services.AddDaprClient();

builder.Build().Run();