using System.Threading.Tasks;
using Identity.GrainInterfaces;
using Identity.Grains;
using Identity.Protos.V1;
using Orleans.TestingHost;
using Xunit;

namespace Identity.Tests;

[Collection("TestClusterCollection")]
public sealed class UserManagementGrainTests {
    private readonly TestCluster _cluster;

    public UserManagementGrainTests(TestClusterFixture fixture) {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task AdminGrain_ShouldActivate_Successfully() {
        // Arrange
        const long userId = 12345;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

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
}
