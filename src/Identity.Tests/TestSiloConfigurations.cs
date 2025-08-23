using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace Identity.Tests;

public sealed class TestSiloConfigurations : ISiloConfigurator {
    public void Configure(ISiloBuilder siloBuilder) {
        _ = siloBuilder.
            AddMemoryGrainStorageAsDefault().
            UseInMemoryReminderService()
            .ConfigureServices(services => {
                // Configure protobuf serialization for Identity.Protos types
                _ = services.AddSerializer(serializerBuilder => {
                    _ = serializerBuilder.AddProtobufSerializer(
                        isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false,
                        isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false);
                });
            });
    }
}
