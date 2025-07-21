using System;

namespace Identity.Infrastructure.Database.DTOs;

/// <summary>
/// Represents a user record in the database.
/// </summary>
public record UserDto {
    public long Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? KycState { get; init; }
    public DateTime? SelfExcludedUntil { get; init; }
    public DateTime InsertedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
