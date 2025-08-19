using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Identity.GrainInterfaces;
using Identity.Grains;
using Identity.Protos.V1;
using Orleans.TestingHost;
using Xunit;

namespace Identity.Tests;

//[Collection(ClusterCollection.Name)]
public class AdminGrainTests {
    //private readonly TestCluster _cluster;

    //public AdminGrainTests(ClusterFixture fixture) {
    //    _cluster = fixture.Cluster;
    //}

    [Fact]
    public async Task AdminGrain_ShouldActivate_Successfully() {
        var builder = new TestClusterBuilder();
        //_ = builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        TestCluster cluster = builder.Build();
        cluster.Deploy();

        // Arrange
        const long userId = 12345;
        IAdminGrain grain = cluster.GrainFactory.GetGrain<IAdminGrain>(userId);

        // Act
        GetUserResponse response = await grain.GetUserAsync();

        cluster.StopAllSilos();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(AdminErrorCodes.Success, response.ErrorInfo.ErrorCode);
        Assert.NotNull(response.User);
        Assert.Equal(userId, response.User.UserId);
        Assert.Contains("player", response.User.Roles);
        Assert.Equal(UserStatus.Active, response.User.Status);
    }

    //public class TestClusterFixture : IDisposable {
    //    public TestCluster Cluster { get; }

    //    public TestClusterFixture() {
    //        var builder = new TestClusterBuilder();
    //        _ = builder.AddSiloBuilderConfigurator<SiloConfigurator>();
    //        Cluster = builder.Build();
    //        Cluster.Deploy();
    //    }

    //    public void Dispose() {
    //        Cluster?.StopAllSilos();
    //        Cluster?.Dispose();
    //    }

    //    private class SiloConfigurator : ISiloConfigurator {
    //        public void Configure(ISiloBuilder siloBuilder) {
    //            _ = siloBuilder.ConfigureServices(services => {
    //                _ = services.AddLogging(builder => builder.AddConsole());

    //                // Configure AdminGrainOptions for testing
    //                _ = services.Configure<AdminGrainOptions>(options => 
    //                    options.CacheExpiry = TimeSpan.FromMinutes(2));
    //            });
    //        }
    //    }
    //}
}
