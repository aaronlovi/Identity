using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using Identity.Infrastructure.Persistence.DTOs;
using Identity.Protos.V1;

namespace Identity.Common.Mappers;

/// <summary>
/// Provides mapping functions for converting between DTOs and domain objects.
/// </summary>
public static class UserMapper {
    /// <summary>
    /// Converts a <see cref="UserDTO"/> to a <see cref="User"/> domain object.
    /// </summary>
    /// <param name="dto">The <see cref="UserDTO"/> to convert.</param>
    /// <returns>A <see cref="User"/> domain object.</returns>
    public static User UserDtoToDomain(UserDTO dto) {
        ArgumentNullException.ThrowIfNull(dto);

        var user = new User {
            UserId = dto.UserId,
            FirebaseUid = dto.FirebaseUid ?? string.Empty,
            Status = ParseUserStatus(dto.Status),
            EmailVerified = true, // Default value; adjust if needed
            CreatedAt = Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            UpdatedAt = Timestamp.FromDateTime(dto.UpdatedAt.ToUniversalTime())
        };
        
        if (dto.Roles != null) {
            user.Roles.Add(dto.Roles);
        }
        
        return user;
    }

    /// <summary>
    /// Converts a <see cref="UserStatus"/> to its string representation for database storage.
    /// </summary>
    /// <param name="status">The <see cref="UserStatus"/> to convert.</param>
    /// <returns>A string representation of the status.</returns>
    public static string UserStatusToDTO(UserStatus status) =>
        status switch {
            UserStatus.Active => "active",
            UserStatus.Banned => "banned",
            UserStatus.ShadowBanned => "shadow_banned",
            UserStatus.Unspecified => "active", // Default to active for unspecified
            _ => "active" // Default fallback
        };

    public static UserStatus UserStatusDtoToDomain(string? userStatus) => ParseUserStatus(userStatus);

    private static UserStatus ParseUserStatus(string? status) {
        if (string.IsNullOrWhiteSpace(status)) {
            return UserStatus.Unspecified;
        }

        return status.ToLowerInvariant() switch {
            "active" => UserStatus.Active,
            "banned" => UserStatus.Banned,
            "shadow_banned" => UserStatus.ShadowBanned,
            _ => UserStatus.Unspecified
        };
    }
}
