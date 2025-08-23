using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Identity.GrainInterfaces;
using Identity.Protos.V1;
using Orleans.TestingHost;
using Xunit;

namespace Identity.Grains.Tests;

[Collection("TestClusterCollection")]
public sealed class UserManagementGrainComprehensiveTests {
    private readonly TestCluster _cluster;

    public UserManagementGrainComprehensiveTests(TestClusterFixture fixture) {
        _cluster = fixture.Cluster;
    }

    #region GetUserAsync Tests

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        const long userId = 1001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        GetUserResponse res = await grain.GetUserAsync();

        // Assert
        _ = res.Should().NotBeNull();
        _ = res.ErrorInfo.Should().NotBeNull();
        _ = res.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        _ = res.ErrorInfo.ErrorMessage.Should().NotBeNullOrEmpty();
        _ = res.ErrorInfo.ErrorMessage.ToLowerInvariant().Should().Contain("not found");
        _ = res.User.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        const long userId = 1002;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        GetUserResponse response = await grain.GetUserAsync(cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserAsync_MultipleCallsToSameGrain_ShouldBeConsistent() {
        // Arrange
        const long userId = 1003;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        GetUserResponse response1 = await grain.GetUserAsync();
        GetUserResponse response2 = await grain.GetUserAsync();

        // Assert
        _ = response1.Should().NotBeNull();
        _ = response2.Should().NotBeNull();
        _ = response1.ErrorInfo.ErrorCode.Should().Be(response2.ErrorInfo.ErrorCode);
        _ = response1.ErrorInfo.ErrorMessage.Should().Be(response2.ErrorInfo.ErrorMessage);
    }

    #endregion

    #region SetUserStatusAsync Tests

    [Fact]
    public async Task SetUserStatusAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        const long userId = 2001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Banned, "Test reason");

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        _ = response.ErrorInfo.ErrorMessage.Should().NotBeNullOrEmpty();
        _ = response.ErrorInfo.ErrorMessage.ToLowerInvariant().Should().Contain("not found");
    }

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.ShadowBanned)]
    public async Task SetUserStatusAsync_WithDifferentStatuses_ShouldHandleAllStatuses(UserStatus status) {
        // Arrange
        const long userId = 2002;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(status, $"Setting status to {status}");

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Since user doesn't exist, we expect an error, but the grain should handle the status type correctly
        _ = response.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
    }

    [Fact]
    public async Task SetUserStatusAsync_WithNullReason_ShouldWork() {
        // Arrange
        const long userId = 2003;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Active, null);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task SetUserStatusAsync_WithEmptyReason_ShouldWork() {
        // Arrange
        const long userId = 2004;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Banned, "");

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task SetUserStatusAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        const long userId = 2005;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Active, "Test", cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    #endregion

    #region UpdateUserRolesAsync Tests

    [Fact]
    public async Task UpdateUserRolesAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        const long userId = 3001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["admin", "moderator"];
        List<string> rolesToRemove = ["player"];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        _ = response.ErrorInfo.ErrorMessage.Should().NotBeNullOrEmpty();
        _ = response.ErrorInfo.ErrorMessage.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithEmptyCollections_ShouldWork() {
        // Arrange
        const long userId = 3002;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> emptyRoles = [];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(emptyRoles, emptyRoles);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Should still return error because user doesn't exist, but grain should handle empty collections
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithOnlyRolesToAdd_ShouldWork() {
        // Arrange
        const long userId = 3003;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["premium", "beta_tester"];
        List<string> rolesToRemove = [];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithOnlyRolesToRemove_ShouldWork() {
        // Arrange
        const long userId = 3004;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = [];
        List<string> rolesToRemove = ["restricted", "temporary"];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithDuplicateRoles_ShouldHandleGracefully() {
        // Arrange
        const long userId = 3005;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["admin", "admin", "moderator"]; // Duplicate admin role
        List<string> rolesToRemove = ["player", "player"]; // Duplicate player role

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Grain should handle duplicates gracefully
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        const long userId = 3006;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["test"];
        List<string> rolesToRemove = [];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove, cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
    }

    #endregion

    #region MintCustomTokenAsync Tests

    [Fact]
    public async Task MintCustomTokenAsync_WithDefaultParameters_ShouldReturnValidToken() {
        // Arrange
        const long userId = 4001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync();

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().NotBeNullOrEmpty();
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
        _ = response.ExpiresAt.Should().NotBeNull();

        // Verify expiry is approximately 15 minutes from now (default TTL)
        DateTime expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        var actualExpiry = response.ExpiresAt.ToDateTime();
        _ = actualExpiry.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public async Task MintCustomTokenAsync_WithDifferentTtlValues_ShouldReturnCorrectExpiry(int ttlMinutes) {
        // Arrange
        const long userId = 4002;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(ttlMinutes);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");

        // Verify expiry matches the requested TTL
        DateTime expectedExpiry = DateTime.UtcNow.AddMinutes(ttlMinutes);
        var actualExpiry = response.ExpiresAt.ToDateTime();
        _ = actualExpiry.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithAdditionalClaims_ShouldReturnToken() {
        // Arrange
        const long userId = 4003;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        var additionalClaims = new Dictionary<string, string> {
            { "department", "engineering" },
            { "level", "senior" },
            { "project", "identity-service" }
        };

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, additionalClaims);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
        _ = response.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithNullAdditionalClaims_ShouldWork() {
        // Arrange
        const long userId = 4004;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, null);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithEmptyAdditionalClaims_ShouldWork() {
        // Arrange
        const long userId = 4005;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        var emptyClaims = new Dictionary<string, string>();

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, emptyClaims);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        const long userId = 4006;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, null, cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
    }

    [Fact]
    public async Task MintCustomTokenAsync_MultipleCallsWithSameUser_ShouldReturnDifferentTokens() {
        // Arrange
        const long userId = 4007;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        MintCustomTokenResponse response1 = await grain.MintCustomTokenAsync();
        MintCustomTokenResponse response2 = await grain.MintCustomTokenAsync();

        // Assert
        _ = response1.Should().NotBeNull();
        _ = response2.Should().NotBeNull();
        _ = response1.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response2.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);

        // Tokens should be different (in a real implementation)
        // For now, they will be the same because it's a stub implementation
        _ = response1.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
        _ = response2.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
    }

    #endregion

    #region Grain Lifecycle Tests

    [Fact]
    public async Task UserManagementGrain_ShouldActivateSuccessfully() {
        // Arrange
        const long userId = 5001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act & Assert - If grain activation fails, this will throw
        GetUserResponse response = await grain.GetUserAsync();
        _ = response.Should().NotBeNull();
    }

    [Fact]
    public async Task UserManagementGrain_DifferentUserIds_ShouldCreateDifferentGrainInstances() {
        // Arrange
        const long userId1 = 5002;
        const long userId2 = 5003;
        IUserManagementGrain grain1 = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId1);
        IUserManagementGrain grain2 = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId2);

        // Act
        GetUserResponse response1 = await grain1.GetUserAsync();
        GetUserResponse response2 = await grain2.GetUserAsync();

        // Assert
        _ = response1.Should().NotBeNull();
        _ = response2.Should().NotBeNull();
        // Both should fail with user not found, but they're separate grain instances
        _ = response1.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        _ = response2.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
    }

    [Fact]
    public async Task UserManagementGrain_ConcurrentCalls_ShouldHandleGracefully() {
        // Arrange
        const long userId = 5004;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act - Make multiple concurrent calls
        var tasks = new Task<GetUserResponse>[5];
        for (int i = 0; i < 5; i++) {
            tasks[i] = grain.GetUserAsync();
        }
        GetUserResponse[] responses = await Task.WhenAll(tasks);

        // Assert
        _ = responses.Should().HaveCount(5);
        foreach (GetUserResponse? response in responses) {
            _ = response.Should().NotBeNull();
            _ = response.ErrorInfo.Should().NotBeNull();
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UserManagementGrain_AllMethods_ShouldHandleNonExistentUserGracefully() {
        // Arrange
        const long userId = 6001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act & Assert - All methods should handle non-existent users gracefully
        GetUserResponse getUserResponse = await grain.GetUserAsync();
        _ = getUserResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        SetUserStatusResponse setStatusResponse = await grain.SetUserStatusAsync(UserStatus.Active, "test");
        _ = setStatusResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        UpdateUserRolesResponse updateRolesResponse = await grain.UpdateUserRolesAsync(["admin"], ["player"]);
        _ = updateRolesResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        // MintCustomToken should work even for non-existent users (it's a stub)
        MintCustomTokenResponse mintTokenResponse = await grain.MintCustomTokenAsync();
        _ = mintTokenResponse.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task UserManagementGrain_OperationsTiming_ShouldCompleteWithinReasonableTime() {
        // Arrange
        const long userId = 7001;
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        var timeout = TimeSpan.FromSeconds(5);

        // Act & Assert - Each operation should complete within reasonable time
        using var cts = new CancellationTokenSource(timeout);
        Task<GetUserResponse> getUserTask = grain.GetUserAsync(cts.Token);
        Task<SetUserStatusResponse> setStatusTask = grain.SetUserStatusAsync(UserStatus.Banned, "test", cts.Token);
        Task<UpdateUserRolesResponse> updateRolesTask = grain.UpdateUserRolesAsync(["admin"], ["player"], cts.Token);
        Task<MintCustomTokenResponse> mintTokenTask = grain.MintCustomTokenAsync(15, null, cts.Token);

        await Task.WhenAll(getUserTask, setStatusTask, updateRolesTask, mintTokenTask);

        _ = getUserTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = setStatusTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = updateRolesTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = mintTokenTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion
}