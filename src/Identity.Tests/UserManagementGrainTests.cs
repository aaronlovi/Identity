using System.Threading.Tasks;
using Identity.GrainInterfaces;
using Identity.Grains;
using Identity.Protos.V1;
using Orleans.TestingHost;
using Xunit;

namespace Identity.Grains.Tests;

[Collection("TestClusterCollection")]
public sealed class UserManagementGrainTests {
    private readonly TestCluster _cluster;

    public UserManagementGrainTests(TestClusterFixture fixture) {
        _cluster = fixture.Cluster;
    }

    [Fact]
    public async Task UserManagementGrain_ShouldActivate_Successfully() {
        // Arrange
        const long userId = 12345;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        GetUserResponse response = await grain.GetUserAsync();

        // Assert - Grain activated successfully and responded
        Assert.NotNull(response);
        Assert.NotNull(response.ErrorInfo);
        
        // The grain should respond successfully even if the user doesn't exist
        // Since we're using in-memory storage that starts empty, we expect an error response
        // This proves the grain activated and can handle requests
        Assert.NotEqual(AdminErrorCodes.Success, response.ErrorInfo.ErrorCode);
        Assert.False(string.IsNullOrEmpty(response.ErrorInfo.ErrorMessage));
        Assert.Contains("not found", response.ErrorInfo.ErrorMessage.ToLowerInvariant());
        Assert.Null(response.User); // No user data should be returned for non-existent user
    }
}
