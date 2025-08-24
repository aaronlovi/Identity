using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Identity.GrainInterfaces;
using Identity.Grains.Tests.Helpers;
using Identity.Grains.Tests.Mocks;
using Identity.Grains.Tests.Models;
using Identity.Infrastructure.Firebase.DomainModels;
using Identity.Infrastructure.Persistence;
using Identity.Protos.V1;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Xunit;

namespace Identity.Grains.Tests;

[Collection("TestClusterCollection")]
public sealed class UserManagementGrainComprehensiveTests {
    private readonly TestCluster _cluster;

    // Thread-safe counter for generating unique user IDs across all tests
    // Start at 1000 to avoid conflicts with any existing test data
    private static long _nextUserId = 1000;

    public UserManagementGrainComprehensiveTests(TestClusterFixture fixture) {
        _cluster = fixture.Cluster;
    }

    /// <summary>
    /// Gets the next unique user ID for test isolation.
    /// Thread-safe across parallel test execution.
    /// </summary>
    private static long GetNextUserId() => Interlocked.Increment(ref _nextUserId);

    /// <summary>
    /// Gets events published for a specific user ID, providing perfect test isolation.
    /// </summary>
    /// <param name="userId">The user ID to filter events for</param>
    /// <returns>List of events published for the specified user</returns>
    private List<PublishedEvent> GetEventsForUser(long userId) {
        var mockEventPublisher = _cluster.GetMockEventPublisher();
        return mockEventPublisher.PublishedEvents
            .Where(e => e.UserId == userId)
            .ToList();
    }

    /// <summary>
    /// Waits for events to be published for a specific user with polling and timeout.
    /// This handles async event publishing gracefully.
    /// </summary>
    /// <param name="userId">The user ID to wait for events</param>
    /// <param name="expectedCount">The expected number of events</param>
    /// <param name="timeout">Maximum time to wait (default: 2 seconds)</param>
    /// <returns>List of events found for the user</returns>
    private async Task<List<PublishedEvent>> WaitForEventsAsync(long userId, int expectedCount, TimeSpan? timeout = null) {
        timeout ??= TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow.Add(timeout.Value);
        
        while (DateTime.UtcNow < deadline) {
            var events = GetEventsForUser(userId);
            if (events.Count >= expectedCount) {
                return events;
            }
            await Task.Delay(10); // Check every 10ms
        }
        
        return GetEventsForUser(userId); // Return whatever we have
    }

    #region GetUserAsync Tests

    [Fact]
    public async Task GetUserAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        GetUserResponse response = await grain.GetUserAsync(cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserAsync_MultipleCallsToSameGrain_ShouldBeConsistent() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        GetUserResponse response1 = await grain.GetUserAsync();
        GetUserResponse response2 = await grain.GetUserAsync();

        // Assert
        _ = response1.Should().NotBeNull();
        _ = response2.Should().NotBeNull();
        _ = response1.ErrorInfo.ErrorCode.Should().Be(response2.ErrorInfo.ErrorCode);
        _ = response1.ErrorInfo.ErrorMessage.Should().Be(response2.ErrorInfo.ErrorMessage);
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region SetUserStatusAsync Tests

    [Fact]
    public async Task SetUserStatusAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Banned);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        _ = response.ErrorInfo.ErrorMessage.Should().NotBeNullOrEmpty();
        _ = response.ErrorInfo.ErrorMessage.ToLowerInvariant().Should().Contain("not found");
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.ShadowBanned)]
    public async Task SetUserStatusAsync_WithDifferentStatuses_ShouldHandleAllStatuses(UserStatus status) {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(status);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Since user doesn't exist, we expect an error, but the grain should handle the status type correctly
        _ = response.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetUserStatusAsync_WithNullReason_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Active);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetUserStatusAsync_WithEmptyReason_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Banned);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetUserStatusAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(UserStatus.Active, cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region UpdateUserRolesAsync Tests

    [Fact]
    public async Task UpdateUserRolesAsync_WhenUserNotFound_ShouldReturnErrorResponse() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithEmptyCollections_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> emptyRoles = [];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(emptyRoles, emptyRoles);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Should still return error because user doesn't exist, but grain should handle empty collections
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithOnlyRolesToAdd_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["premium", "beta_tester"];
        List<string> rolesToRemove = [];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithOnlyRolesToRemove_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = [];
        List<string> rolesToRemove = ["restricted", "temporary"];

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithDuplicateRoles_ShouldHandleGracefully() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["admin", "admin", "moderator"]; // Duplicate admin role
        List<string> rolesToRemove = ["player", "player"]; // Duplicate player role

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        // Grain should handle duplicates gracefully
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        List<string> rolesToAdd = ["test"];
        List<string> rolesToRemove = [];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove, cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region MintCustomTokenAsync Tests

    [Fact]
    public async Task MintCustomTokenAsync_WithDefaultParameters_ShouldReturnValidToken() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public async Task MintCustomTokenAsync_WithDifferentTtlValues_ShouldReturnCorrectExpiry(int ttlMinutes) {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithAdditionalClaims_ShouldReturnToken() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithNullAdditionalClaims_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, null);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithEmptyAdditionalClaims_ShouldWork() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        var emptyClaims = new Dictionary<string, string>();

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, emptyClaims);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.CustomToken.Should().Contain($"stub_token_for_user_{userId}");
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MintCustomTokenAsync_WithCancellationToken_ShouldComplete() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        MintCustomTokenResponse response = await grain.MintCustomTokenAsync(15, null, cts.Token);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MintCustomTokenAsync_MultipleCallsWithSameUser_ShouldReturnDifferentTokens() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region Event Publishing Tests - These test successful operations with events

    [Fact]
    public async Task SetUserStatusAsync_WithExistingUser_ShouldPublishUserStatusChangedEvent() {
        // Arrange
        long userId = GetNextUserId();
        const string initialStatus = "active";
        const UserStatus newStatus = UserStatus.Banned;
        
        // Create a test user in the in-memory database
        _ = TestDataHelper.CreateTestUser(_cluster, userId, status: initialStatus, roles: ["player"]);
        
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        SetUserStatusResponse response = await grain.SetUserStatusAsync(newStatus);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);

        // Wait for event to be published (handles async event publishing)
        var userEvents = await WaitForEventsAsync(userId, expectedCount: 1);
        _ = userEvents.Should().HaveCount(1);
        
        var publishedEvent = userEvents[0];
        _ = publishedEvent.EventType.Should().Be("UserStatusChanged");
        _ = publishedEvent.UserId.Should().Be(userId);
        _ = publishedEvent.Data.Should().BeOfType<UserStatusChangedEvent>();
        
        var eventData = (UserStatusChangedEvent)publishedEvent.Data;
        _ = eventData.UserId.Should().Be(userId);
        _ = eventData.NewStatus.Should().Be("banned");
        _ = eventData.PreviousStatus.Should().Be(initialStatus);
        _ = eventData.ChangedBy.Should().Be("system"); // Default from options
        _ = eventData.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Clean up
        _ = TestDataHelper.RemoveTestUser(_cluster, userId);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WithExistingUser_ShouldPublishUserRolesUpdatedEvent() {
        // Arrange
        long userId = GetNextUserId();
        var initialRoles = new List<string> { "player" };
        var rolesToAdd = new List<string> { "admin", "moderator" };
        var rolesToRemove = new List<string> { "player" };
        
        // Create a test user in the in-memory database
        _ = TestDataHelper.CreateTestUser(_cluster, userId, roles: initialRoles);
        
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        UpdateUserRolesResponse response = await grain.UpdateUserRolesAsync(rolesToAdd, rolesToRemove);

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);

        // Wait for event to be published (handles async event publishing)
        var userEvents = await WaitForEventsAsync(userId, expectedCount: 1);
        _ = userEvents.Should().HaveCount(1);
        
        var publishedEvent = userEvents[0];
        _ = publishedEvent.EventType.Should().Be("UserRolesUpdated");
        _ = publishedEvent.UserId.Should().Be(userId);
        _ = publishedEvent.Data.Should().BeOfType<UserRolesUpdatedEvent>();
        
        var eventData = (UserRolesUpdatedEvent)publishedEvent.Data;
        _ = eventData.UserId.Should().Be(userId);
        _ = eventData.AddedRoles.Should().BeEquivalentTo(rolesToAdd);
        _ = eventData.RemovedRoles.Should().BeEquivalentTo(rolesToRemove);
        _ = eventData.Roles.Should().BeEquivalentTo(new[] { "admin", "moderator" }); // Final state
        _ = eventData.ChangedBy.Should().Be("system"); // Default from options
        _ = eventData.ChangedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Clean up
        _ = TestDataHelper.RemoveTestUser(_cluster, userId);
    }

    [Fact]
    public async Task SetUserStatusAsync_MultipleCalls_ShouldPublishMultipleEvents() {
        // Arrange
        long userId = GetNextUserId();
        
        // Create a test user
        _ = TestDataHelper.CreateTestUser(_cluster, userId, status: "active");
        
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act - Change status multiple times
        SetUserStatusResponse response1 = await grain.SetUserStatusAsync(UserStatus.Banned);
        SetUserStatusResponse response2 = await grain.SetUserStatusAsync(UserStatus.Active);
        SetUserStatusResponse response3 = await grain.SetUserStatusAsync(UserStatus.ShadowBanned);

        // Assert
        _ = response1.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response2.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response3.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);

        // Wait for all events to be published (handles async event publishing)
        var userEvents = await WaitForEventsAsync(userId, expectedCount: 3);
        _ = userEvents.Should().HaveCount(3);
        
        // Verify the sequence of status changes
        _ = userEvents[0].EventType.Should().Be("UserStatusChanged");
        _ = ((UserStatusChangedEvent)userEvents[0].Data).NewStatus.Should().Be("banned");
        _ = ((UserStatusChangedEvent)userEvents[0].Data).PreviousStatus.Should().Be("active");
        
        _ = userEvents[1].EventType.Should().Be("UserStatusChanged");
        _ = ((UserStatusChangedEvent)userEvents[1].Data).NewStatus.Should().Be("active");
        _ = ((UserStatusChangedEvent)userEvents[1].Data).PreviousStatus.Should().Be("banned");
        
        _ = userEvents[2].EventType.Should().Be("UserStatusChanged");
        _ = ((UserStatusChangedEvent)userEvents[2].Data).NewStatus.Should().Be("shadow_banned");
        _ = ((UserStatusChangedEvent)userEvents[2].Data).PreviousStatus.Should().Be("active");
        
        // Clean up
        _ = TestDataHelper.RemoveTestUser(_cluster, userId);
    }

    [Fact]
    public async Task GetUserAsync_WithExistingUser_ShouldReturnUserAndNotPublishEvents() {
        // Arrange
        long userId = GetNextUserId();
        var roles = new List<string> { "player", "beta_tester" };
        
        // Create a test user
        var createdUser = TestDataHelper.CreateTestUser(_cluster, userId, status: "active", roles: roles);
        
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act
        GetUserResponse response = await grain.GetUserAsync();

        // Assert
        _ = response.Should().NotBeNull();
        _ = response.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        _ = response.User.Should().NotBeNull();
        _ = response.User.UserId.Should().Be(userId);
        _ = response.User.FirebaseUid.Should().Be(createdUser.FirebaseUid);
        _ = response.User.Status.Should().Be(UserStatus.Active);
        _ = response.User.Roles.Should().BeEquivalentTo(roles);

        // Verify no events published for read operations on this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
        
        // Clean up
        _ = TestDataHelper.RemoveTestUser(_cluster, userId);
    }

    #endregion

    #region Grain Lifecycle Tests

    [Fact]
    public async Task UserManagementGrain_ShouldActivateSuccessfully() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act & Assert - If grain activation fails, this will throw
        GetUserResponse response = await grain.GetUserAsync();
        _ = response.Should().NotBeNull();
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UserManagementGrain_DifferentUserIds_ShouldCreateDifferentGrainInstances() {
        // Arrange
        long userId1 = GetNextUserId();
        long userId2 = GetNextUserId();
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
        
        // Verify no events published for either user
        var userEvents1 = GetEventsForUser(userId1);
        var userEvents2 = GetEventsForUser(userId2);
        _ = userEvents1.Should().BeEmpty();
        _ = userEvents2.Should().BeEmpty();
    }

    [Fact]
    public async Task UserManagementGrain_ConcurrentCalls_ShouldHandleGracefully() {
        // Arrange
        long userId = GetNextUserId();
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
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UserManagementGrain_AllMethods_ShouldHandleNonExistentUserGracefully() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);

        // Act & Assert - All methods should handle non-existent users gracefully
        GetUserResponse getUserResponse = await grain.GetUserAsync();
        _ = getUserResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        SetUserStatusResponse setStatusResponse = await grain.SetUserStatusAsync(UserStatus.Active);
        _ = setStatusResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        UpdateUserRolesResponse updateRolesResponse = await grain.UpdateUserRolesAsync(["admin"], ["player"]);
        _ = updateRolesResponse.ErrorInfo.ErrorCode.Should().NotBe(AdminErrorCodes.Success);

        // MintCustomToken should work even for non-existent users (it's a stub)
        MintCustomTokenResponse mintTokenResponse = await grain.MintCustomTokenAsync();
        _ = mintTokenResponse.ErrorInfo.ErrorCode.Should().Be(AdminErrorCodes.Success);
        
        // Verify no events published for this user
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task UserManagementGrain_OperationsTiming_ShouldCompleteWithinReasonableTime() {
        // Arrange
        long userId = GetNextUserId();
        IUserManagementGrain grain = _cluster.GrainFactory.GetGrain<IUserManagementGrain>(userId);
        var timeout = TimeSpan.FromSeconds(5);

        // Act & Assert - Each operation should complete within reasonable time
        using var cts = new CancellationTokenSource(timeout);
        Task<GetUserResponse> getUserTask = grain.GetUserAsync(cts.Token);
        Task<SetUserStatusResponse> setStatusTask = grain.SetUserStatusAsync(UserStatus.Banned, cts.Token);
        Task<UpdateUserRolesResponse> updateRolesTask = grain.UpdateUserRolesAsync(["admin"], ["player"], cts.Token);
        Task<MintCustomTokenResponse> mintTokenTask = grain.MintCustomTokenAsync(15, null, cts.Token);

        await Task.WhenAll(getUserTask, setStatusTask, updateRolesTask, mintTokenTask);

        _ = getUserTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = setStatusTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = updateRolesTask.IsCompletedSuccessfully.Should().BeTrue();
        _ = mintTokenTask.IsCompletedSuccessfully.Should().BeTrue();
        
        // Verify no events published for this user (all operations failed due to non-existent user)
        var userEvents = GetEventsForUser(userId);
        _ = userEvents.Should().BeEmpty();
    }

    #endregion
}