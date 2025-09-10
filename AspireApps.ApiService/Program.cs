using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisOutputCache("cache");

builder.Services.AddDaprClient();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCloudEvents();

app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries =
    ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");


// app
//     .MapPost("/orderscreated", ([FromBody] Order order) => Results.Ok($"{order.Id} {order.Sku} processed!"))
//     .WithTopic("pubsub", "orders.created");

app.MapPost("/orderscreated", ([FromBody] Order order) =>
{
    return Results.Ok($"{order.Id} {order.Sku} processed!");
}).WithTopic("pubsub-rabbit", "orders.created");

app.MapGet("/orders/{id:int}", (int id) =>
    new Order { Id = id, Price = 12, Sku = "MY-SKU" });



app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}