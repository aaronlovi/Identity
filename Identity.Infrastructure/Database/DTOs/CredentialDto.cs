using System;

namespace Identity.Infrastructure.Database.DTOs;

/// <summary>
/// Represents a credential record in the database.
/// </summary>
public record CredentialDto {
    public long UserId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? ExternalId { get; init; }
    public string? Hash { get; init; }
    public string? MfaSecret { get; init; }
    public DateTime InsertedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
