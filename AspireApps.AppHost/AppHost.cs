using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithDbGate();

var daprState = builder.AddDaprStateStore("statestore");

var sideCarOptions = new DaprSidecarOptions
{
    EnableAppHealthCheck = true,
    AppHealthCheckPath = "/health",
    //ResourcesPaths = [Path.Combine("..", "components")]
};

IResourceBuilder<IDaprComponentResource> daprPubSubBuilder = null;
IResourceBuilder<RabbitMQServerResource>? rabbitmq = null;
var pubSubType = "pubsub.azure.servicebus";

if (builder.Environment.IsDevelopment())
{
    const int rabbitPort = 5555;
    var username = builder.AddParameterFromConfiguration("rabbitmq-username", "guest", true);
    var password = builder.AddParameterFromConfiguration("rabbitmq-password", "guest", true);
    rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, rabbitPort)
        //.WithEndpoint(targetPort: rabbitPort)
        .WithManagementPlugin();

    pubSubType = "pubsub.rabbitmq";

    daprPubSubBuilder = builder
        .AddDaprPubSub("pubsub-rabbit")
        .WithMetadata("connectionString", $"amqp://localhost:{rabbitPort}")
        .WaitFor(rabbitmq);
}

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