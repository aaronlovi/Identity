using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Infrastructure.Persistence.DTOs;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbmInMemoryService : DbmInMemoryService, IIdentityDbmService, IDbmService {
    private readonly ILogger<IdentityDbmInMemoryService> _logger;

    private readonly IdentityDbmInMemoryData _data;

    public IdentityDbmInMemoryService(ILoggerFactory loggerFactory)
        : base(loggerFactory) {
        _logger = loggerFactory.CreateLogger<IdentityDbmInMemoryService>();
        _data = new();
        _logger.LogWarning("DbmInMemory is instantiated: persistence in RAM only");
    }

    public Task<Result<UserDTO>> GetUser(long userId, CancellationToken ct) {
        lock (Locker) {
            UserDTO? user = _data.GetUser(userId);

            if (user is null) {
                _logger.LogWarning("GetUser failed: User not found");
                return Task.FromResult(Result<UserDTO>.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }

            _logger.LogInformation("GetUser success");
            return Task.FromResult(Result<UserDTO>.Success(user));
        }
    }

    public Task<Result> SetUserStatus(long userId, string status, CancellationToken ct) {
        lock (Locker) {
            bool updated = _data.SetUserStatus(userId, status);
            if (!updated) {
                _logger.LogWarning("SetUserStatus failed: User not found");
                return Task.FromResult(Result.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }
            _logger.LogInformation("SetUserStatus success");
            return Task.FromResult(Result.Success);
        }
    }

    public Task<Result> UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove, CancellationToken ct) {
        lock (Locker) {
            bool updated = _data.UpdateUserRoles(userId, rolesToAdd, rolesToRemove);
            if (!updated) {
                _logger.LogWarning("UpdateUserRoles failed: User not found");
                return Task.FromResult(Result.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }
            _logger.LogInformation("UpdateUserRoles success");
            return Task.FromResult(Result.Success);
        }
    }

    #region Test Support Methods

    /// <summary>
    /// Adds a user to the in-memory data store. Used for testing purposes only.
    /// If a user with the same userId already exists, it will be replaced.
    /// </summary>
    /// <param name="user">The user to add</param>
    public void AddTestUser(UserDTO user) {
        lock (Locker) {
            _data.AddUser(user);
            _logger.LogDebug("Test user added: {UserId}", user.UserId);
        }
    }

    /// <summary>
    /// Removes a user from the in-memory data store. Used for testing purposes only.
    /// </summary>
    /// <param name="userId">The ID of the user to remove</param>
    /// <returns>True if the user was found and removed, false otherwise</returns>
    public bool RemoveTestUser(long userId) {
        lock (Locker) {
            bool removed = _data.RemoveUser(userId);
            if (removed) {
                _logger.LogDebug("Test user removed: {UserId}", userId);
            }
            return removed;
        }
    }

    /// <summary>
    /// Removes all users from the in-memory data store. Used for testing purposes only.
    /// </summary>
    public void ClearTestUsers() {
        lock (Locker) {
            _data.Clear();
            _logger.LogDebug("All test users cleared");
        }
    }

    /// <summary>
    /// Gets the total count of users in the in-memory data store. Used for testing purposes only.
    /// </summary>
    public int TestUserCount {
        get {
            lock (Locker) {
                return _data.UserCount;
            }
        }
    }

    #endregion
}
