using Microsoft.Extensions.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;
using Orleans.TestingHost;

namespace Identity.Grains.Tests;

public sealed class TestClientConfigurations : IClientBuilderConfigurator {
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) {
        _ = clientBuilder.ConfigureServices(services => {
            // Configure protobuf serialization for Identity.Protos types
            _ = services.AddSerializer(serializerBuilder => {
                _ = serializerBuilder.AddProtobufSerializer(
                    isSerializable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false,
                    isCopyable: type => type.Namespace?.StartsWith("Identity.Protos") ?? false);
            });
        });
    }
}
