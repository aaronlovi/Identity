using System;
using System.Collections.Generic;

namespace Identity.Infrastructure.Persistence.DTOs;

public record UserDTO(
    long UserId,
    string FirebaseUid,
    List<string> Roles,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt) {
    public static UserDTO Empty => new(
        UserId: 0,
        FirebaseUid: string.Empty,
        Roles: [],
        Status: string.Empty,
        CreatedAt: DateTime.MinValue,
        UpdatedAt: DateTime.MinValue);

    public bool IsEmpty => UserId == 0;
}
