using System;
using System.Collections.Generic;
using System.Linq;
using Identity.Infrastructure.Persistence.DTOs;

namespace Identity.Infrastructure.Persistence;

internal class IdentityDbmInMemoryData {
    // Static shared storage that persists across all instances
    private static readonly Dictionary<long, UserDTO> _sharedUsersByUserId = [];
    private static readonly object _lock = new object();

    internal UserDTO? GetUser(long userId) {
        lock (_lock) {
            _ = _sharedUsersByUserId.TryGetValue(userId, out UserDTO? user);
            return user;
        }
    }

    internal bool SetUserStatus(long userId, string status) {
        lock (_lock) {
            _ = _sharedUsersByUserId.TryGetValue(userId, out UserDTO? user);
            if (user is null)
                return false;
            
            user = user with {
                Status = status,
                UpdatedAt = DateTime.UtcNow
            };
            _sharedUsersByUserId[userId] = user;
            return true;
        }
    }

    internal bool UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove) {
        lock (_lock) {
            if (!_sharedUsersByUserId.TryGetValue(userId, out UserDTO? user))
                return false;

            var currentRoles = user.Roles.ToList();

            foreach (string role in rolesToAdd) {
                if (!currentRoles.Contains(role))
                    currentRoles.Add(role);
            }

            foreach (string role in rolesToRemove)
                _ = currentRoles.Remove(role);

            _sharedUsersByUserId[userId] = user with { Roles = currentRoles };

            return true;
        }
    }

    /// <summary>
    /// Adds a user to the shared in-memory data store. Used for testing purposes.
    /// If a user with the same userId already exists, it will be replaced.
    /// </summary>
    /// <param name="user">The user to add</param>
    internal void AddUser(UserDTO user) {
        lock (_lock) {
            _sharedUsersByUserId[user.UserId] = user;
        }
    }

    /// <summary>
    /// Removes a user from the shared in-memory data store. Used for testing purposes.
    /// </summary>
    /// <param name="userId">The ID of the user to remove</param>
    /// <returns>True if the user was found and removed, false otherwise</returns>
    internal bool RemoveUser(long userId) {
        lock (_lock) {
            return _sharedUsersByUserId.Remove(userId);
        }
    }

    /// <summary>
    /// Removes all users from the shared in-memory data store. Used for testing purposes.
    /// </summary>
    internal void Clear() {
        lock (_lock) {
            _sharedUsersByUserId.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of users in the shared in-memory data store. Used for testing purposes.
    /// </summary>
    internal int UserCount {
        get {
            lock (_lock) {
                return _sharedUsersByUserId.Count;
            }
        }
    }
}
