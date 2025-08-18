using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Identity.Infrastructure;
using Identity.Grains;
using InnoAndLogic.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Hosting = Microsoft.Extensions.Hosting;

namespace Identity.Host;

internal class Program {
    private static async Task Main(string[] args) {
        // Build the host with Orleans silo
        IHostBuilder hostBuilder = Hosting.Host.CreateDefaultBuilder(args)
            .UseOrleans((context, siloBuilder) => {
                // Use localhost clustering for local development
                _ = siloBuilder.UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseDashboard(options => { }); // Optional: Orleans Dashboard for monitoring
            })
            .ConfigureAppConfiguration((context, config) => {
                _ = config.
                    AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
                    AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true).
                    AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) => {
                var migrationAssemblies = new List<Assembly> {
                    typeof(Infrastructure.ServiceCollectionExtensions).Assembly
                };
                _ = services.AddInfrastructureServices(context.Configuration, migrationAssemblies);
                
                // Configure AdminGrain options
                _ = services.Configure<AdminGrainOptions>(context.Configuration.GetSection("AdminGrainOptions"));
            })
            .UseConsoleLifetime();

        IHost host = hostBuilder.Build();

        // Resolve DbmService from DI container and test migrations
        using (IServiceScope scope = host.Services.CreateScope()) {
            IDbmService dbmService = scope.ServiceProvider.GetRequiredService<IDbmService>();
            Console.WriteLine("DbmService resolved and migrations tested.");
        }

        // Start the Orleans silo
        Console.WriteLine("Starting Orleans silo...");
        await host.StartAsync();

        Console.WriteLine("Orleans silo is running. Press Ctrl+C to shut down.");

        // Wait for shutdown
        await host.WaitForShutdownAsync();

        Console.WriteLine("Orleans silo stopped.");
    }
}