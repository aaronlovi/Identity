using System;
using System.Collections.Generic;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Identity.Grains.Tests.Helpers;

/// <summary>
/// Helper class for managing test data in the in-memory database during tests.
/// </summary>
public static class TestDataHelper {
    /// <summary>
    /// Creates a test user with the specified parameters and adds it to the in-memory database.
    /// </summary>
    /// <param name="cluster">The test cluster</param>
    /// <param name="userId">User ID</param>
    /// <param name="firebaseUid">Firebase UID (optional, will generate if not provided)</param>
    /// <param name="status">User status (default: "active")</param>
    /// <param name="roles">User roles (default: ["player"])</param>
    /// <returns>The created UserDTO</returns>
    public static UserDTO CreateTestUser(
        TestCluster cluster,
        long userId,
        string? firebaseUid = null,
        string status = "active",
        List<string>? roles = null) {
        
        var siloServiceProvider = cluster.GetSiloServiceProvider();
        var dbService = siloServiceProvider.GetRequiredService<IIdentityDbmService>();
        
        if (dbService is not IdentityDbmInMemoryService inMemoryService) {
            throw new InvalidOperationException("Test data seeding is only supported with IdentityDbmInMemoryService");
        }

        var user = new UserDTO(
            UserId: userId,
            FirebaseUid: firebaseUid ?? $"firebase_uid_{userId}",
            Status: status,
            Roles: roles ?? ["player"],
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        inMemoryService.AddTestUser(user);
        return user;
    }

    /// <summary>
    /// Removes a test user from the in-memory database.
    /// </summary>
    /// <param name="cluster">The test cluster</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>True if the user was found and removed, false otherwise</returns>
    public static bool RemoveTestUser(TestCluster cluster, long userId) {
        var siloServiceProvider = cluster.GetSiloServiceProvider();
        var dbService = siloServiceProvider.GetRequiredService<IIdentityDbmService>();
        
        if (dbService is not IdentityDbmInMemoryService inMemoryService) {
            throw new InvalidOperationException("Test data management is only supported with IdentityDbmInMemoryService");
        }

        return inMemoryService.RemoveTestUser(userId);
    }

    /// <summary>
    /// Clears all test users from the in-memory database.
    /// </summary>
    /// <param name="cluster">The test cluster</param>
    public static void ClearAllTestUsers(TestCluster cluster) {
        var siloServiceProvider = cluster.GetSiloServiceProvider();
        var dbService = siloServiceProvider.GetRequiredService<IIdentityDbmService>();
        
        if (dbService is not IdentityDbmInMemoryService inMemoryService) {
            throw new InvalidOperationException("Test data management is only supported with IdentityDbmInMemoryService");
        }

        inMemoryService.ClearTestUsers();
    }

    /// <summary>
    /// Gets the count of test users in the in-memory database.
    /// </summary>
    /// <param name="cluster">The test cluster</param>
    /// <returns>The number of users in the database</returns>
    public static int GetTestUserCount(TestCluster cluster) {
        var siloServiceProvider = cluster.GetSiloServiceProvider();
        var dbService = siloServiceProvider.GetRequiredService<IIdentityDbmService>();
        
        if (dbService is not IdentityDbmInMemoryService inMemoryService) {
            throw new InvalidOperationException("Test data management is only supported with IdentityDbmInMemoryService");
        }

        return inMemoryService.TestUserCount;
    }
}