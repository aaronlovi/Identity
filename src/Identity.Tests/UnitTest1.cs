using System;
using System.Threading.Tasks;
using Identity.Grains;
using Identity.Protos.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Identity.Tests;

public class AdminGrainTests : IClassFixture<AdminGrainTests.TestClusterFixture> {
    private readonly TestCluster _cluster;

    public AdminGrainTests(TestClusterFixture fixture) {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task AdminGrain_ShouldActivate_Successfully() {
        // Arrange
        const long userId = 12345;
        IAdminGrain grain = _cluster.GrainFactory.GetGrain<IAdminGrain>(userId);

        // Act
        GetUserResponse response = await grain.GetUserAsync();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(AdminErrorCodes.Success, response.ErrorInfo.ErrorCode);
        Assert.NotNull(response.User);
        Assert.Equal(userId, response.User.UserId);
        Assert.Contains("player", response.User.Roles);
        Assert.Equal(UserStatus.Active, response.User.Status);
    }

    public class TestClusterFixture : IDisposable {
        public TestCluster Cluster { get; }

        public TestClusterFixture() {
            var builder = new TestClusterBuilder();
            _ = builder.AddSiloBuilderConfigurator<SiloConfigurator>();
            Cluster = builder.Build();
            Cluster.Deploy();
        }

        public void Dispose() {
            Cluster?.StopAllSilos();
            Cluster?.Dispose();
        }

        private class SiloConfigurator : ISiloConfigurator {
            public void Configure(ISiloBuilder siloBuilder) {
                _ = siloBuilder.ConfigureServices(services => {
                    _ = services.AddLogging(builder => builder.AddConsole());

                    // Configure AdminGrainOptions for testing
                    _ = services.Configure<AdminGrainOptions>(options => {
                        options.CacheExpiry = TimeSpan.FromMinutes(2); // Shorter for tests
                    });
                });
            }
        }
    }
}
