using System;
using System.Collections.Generic;
using System.Linq;
using Identity.Infrastructure.Persistence.DTOs;

namespace Identity.Infrastructure.Persistence;

internal class IdentityDbmInMemoryData {
    private readonly Dictionary<long, UserDTO> _usersByUserId;

    public IdentityDbmInMemoryData() {
        _usersByUserId = [];
    }

    internal UserDTO? GetUser(long userId) {
        _ = _usersByUserId.TryGetValue(userId, out UserDTO? user);
        return user;
    }

    internal bool SetUserStatus(long userId, string status) {
        _ = _usersByUserId.TryGetValue(userId, out UserDTO? user);
        if (user is null)
            return false;
        
        user = user with {
            Status = status,
            UpdatedAt = DateTime.UtcNow
        };
        _usersByUserId[userId] = user;
        return true;
    }

    internal bool UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove) {
        if (!_usersByUserId.TryGetValue(userId, out UserDTO? user))
            return false;

        var currentRoles = user.Roles.ToList();

        foreach (string role in rolesToAdd) {
            if (!currentRoles.Contains(role))
                currentRoles.Add(role);
        }

        foreach (string role in rolesToRemove)
            _ = currentRoles.Remove(role);

        _usersByUserId[userId] = user with { Roles = currentRoles };

        return true;
    }
}
