using System;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a credential in the domain.
/// </summary>
public class Credential {
    private string? _externalId;
    private PasswordHash? _hash;
    private string? _mfaSecret;

    public Credential(long userId, CredentialType type, string? externalId, PasswordHash? hash, string? mfaSecret, DateTime insertedAt, DateTime updatedAt) {
        UserId = userId;
        Type = type;
        _externalId = externalId;
        _hash = hash;
        _mfaSecret = mfaSecret;
        InsertedAt = insertedAt;
        UpdatedAt = updatedAt;
    }

    public long UserId { get; private set; }
    public CredentialType Type { get; private set; }
    public string? ExternalId {
        get => _externalId;
        set {
            _externalId = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public PasswordHash? Hash {
        get => _hash;
        set {
            _hash = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public string? MfaSecret {
        get => _mfaSecret;
        set {
            _mfaSecret = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public DateTime InsertedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}