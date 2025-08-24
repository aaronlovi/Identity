using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Identity.GrainInterfaces;
using Identity.Grains;
using Identity.Infrastructure;
using Identity.Infrastructure.Firebase;
using Identity.Infrastructure.Persistence;
using InnoAndLogic.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Serialization;
using Hosting = Microsoft.Extensions.Hosting;

namespace Identity.Host;

internal class Program {
    private static async Task Main(string[] args) {
        string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // Build the host with Orleans silo
        IHostBuilder hostBuilder = Hosting.Host.CreateDefaultBuilder(args)
            .UseOrleans((context, siloBuilder) => {
                // Use localhost clustering for local development
                _ = siloBuilder.UseLocalhostClustering()
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseDashboard(options => { })
                    .ConfigureServices(services => {
                        // Configure protobuf serialization for Identity.Protos types
                        _ = services.AddSerializer(serializerBuilder => {
                            _ = serializerBuilder.AddProtobufSerializer(
                                isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") == true,
                                isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") == true);
                        });
                    }); // Optional: Orleans Dashboard for monitoring
            })
            .ConfigureAppConfiguration((context, config) => {
                _ = config.
                    AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
                    AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true).
                    AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) => {
                var migrationAssemblies = new List<Assembly> {
                    typeof(Infrastructure.ServiceCollectionExtensions).Assembly
                };
                _ = services.ConfigureIdentityPersistenceServices(
                    context.Configuration,
                    "DbmOptions",
                    migrationAssemblies);

                _ = services.ConfigureFirebase(context.Configuration, "FirebaseOptions");

                // Configure AdminGrain options
                _ = services.Configure<UserManagementGrainOptions>(context.Configuration.GetSection("AdminGrainOptions"));
            })
            .ConfigureLogging((context, builder) => builder.ClearProviders())
            .UseConsoleLifetime();

        IHost host = hostBuilder.Build();

        // Resolve DbmService from DI container and test migrations
        using (IServiceScope scope = host.Services.CreateScope()) {
            IIdentityDbmService dbmService = scope.ServiceProvider.GetRequiredService<IIdentityDbmService>();
            Console.WriteLine("DbmService resolved and migrations tested.");
        }

        // Start the Orleans silo
        Console.WriteLine("Starting Orleans silo...");
        await host.StartAsync();

        Console.WriteLine("Orleans silo is running. Press Ctrl+C to shut down.");

        IGrainFactory grainFactory = host.Services.GetRequiredService<IGrainFactory>();
        IUserManagementGrain adminGrain = grainFactory.GetGrain<IUserManagementGrain>(12345);

        Protos.V1.GetUserResponse x = await adminGrain.GetUserAsync();

        // Wait for shutdown
        await host.WaitForShutdownAsync();

        Console.WriteLine("Orleans silo stopped.");
    }
}