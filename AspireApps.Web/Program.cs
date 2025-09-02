using AspireApps.Web;
using AspireApps.Web.Components;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddDaprClient();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapGet("/orders/{id:int}", async (int id, [FromServices] DaprClient dapr) =>
    await dapr.InvokeMethodAsync<Order>(HttpMethod.Get, "apiservice", $"orders/{id}"));

app.MapPost("/publish", async ([FromServices] DaprClient dapr) =>
{
    var order = new Order{ Id = 123, Price = 12, Sku = "MY-SKU" };
    await dapr.PublishEventAsync("pubsub", "orders.created", order);
    return Results.Ok("published");
});

app.UseHttpsRedirection();

app.UseCloudEvents();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapSubscribeHandler();

app.MapDefaultEndpoints();

app.Run();