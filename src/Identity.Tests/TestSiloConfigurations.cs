using Identity.Grains.Tests.Mocks;
using Identity.Infrastructure.Firebase;
using Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace Identity.Grains.Tests;

public sealed class TestSiloConfigurations : ISiloConfigurator {
    public void Configure(ISiloBuilder siloBuilder) {
        // Build configuration manually since we don't have access to hosting context
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Test.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        _ = siloBuilder
            .AddMemoryGrainStorageAsDefault()
            .UseInMemoryReminderService()
            .ConfigureServices(services => {
                _ = services.ConfigureIdentityPersistenceServices(
                    configuration, "DbmOptions");

                // Configure UserManagementGrain options
                _ = services.Configure<UserManagementGrainOptions>(
                    configuration.GetSection("UserManagementGrainOptions"));

                // Register Mock EventPublisher for tests
                _ = services.AddSingleton<IEventPublisher, MockEventPublisher>();

                // Configure protobuf serialization for Identity.Protos types
                _ = services.AddSerializer(serializerBuilder => {
                    _ = serializerBuilder.AddProtobufSerializer(
                        isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false,
                        isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false);
                });
            });
    }
}
