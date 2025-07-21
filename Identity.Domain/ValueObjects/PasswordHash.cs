using System;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Represents a hashed password.
/// </summary>
public record PasswordHash {
    public PasswordHash(string hash) {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash cannot be null or empty.", nameof(hash));

        Hash = hash;
    }

    public string Hash { get; init; }
}