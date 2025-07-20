using System;
using System.Linq;
using Identity.Api.Middleware;
using Identity.Domain;
using Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

internal class Program {
    private static void Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add Orleans hosting
        _ = builder.Host.UseOrleans(silo => silo.UseLocalhostClustering());

        // Register Domain and Infrastructure services for DI
        _ = builder.Services.
                AddDomainServices(). // Extension method for Identity.Domain
                AddInfrastructureServices(builder.Configuration); // Extension method for Identity.Infrastructure

        WebApplication app = builder.Build();

        // Use development-only middleware
        if (app.Environment.IsDevelopment())
            _ = app.UseMiddleware<DevelopmentOnlyMiddleware>();

        _ = app.UseHttpsRedirection();

        string[] summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        _ = app.MapGet("/weatherforecast", () => {
            WeatherForecast[] forecast = [.. Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))];
            return forecast;
        })
        .WithName("GetWeatherForecast");

        app.Run();
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) {
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
