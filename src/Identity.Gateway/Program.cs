using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;

namespace Identity.Gateway;

internal class Program {
    private static void Main(string[] args) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddSwaggerGen();

        // Add Orleans client
        _ = builder.Host.UseOrleansClient(clientBuilder =>
            clientBuilder.UseLocalhostClustering());

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            _ = app.UseSwagger();
            _ = app.UseSwaggerUI();
        }

        _ = app.UseHttpsRedirection();

        string[] summaries =
            ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

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

        // Add a simple health check endpoint to verify Orleans client connectivity
        _ = app.MapGet("/health", async (IClusterClient clusterClient) => {
            try {
                // Try to get the management grain to verify connectivity
                IManagementGrain managementGrain = clusterClient.GetGrain<IManagementGrain>(0);
                _ = await managementGrain.GetHosts();
                return Results.Ok(new { status = "healthy", orleans = "connected" });
            } catch (Exception ex) {
                return Results.Problem($"Orleans client not connected: {ex.Message}");
            }
        })
        .WithName("HealthCheck");

        Console.WriteLine("Starting Identity Gateway...");
        app.Run();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) {
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
