using System;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a user in the domain.
/// </summary>
public class User {
    private UserStatus _status;
    private string? _kycState;
    private DateTime? _selfExcludedUntil;

    public User(long id, UserStatus status, string? kycState, DateTime? selfExcludedUntil, DateTime insertedAt, DateTime updatedAt) {
        Id = id;
        _status = status;
        _kycState = kycState;
        _selfExcludedUntil = selfExcludedUntil;
        InsertedAt = insertedAt;
        UpdatedAt = updatedAt;
    }

    // Public properties
    public long Id { get; private set; }
    public UserStatus Status {
        get => _status;
        set {
            _status = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public string? KycState {
        get => _kycState;
        set {
            _kycState = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public DateTime? SelfExcludedUntil {
        get => _selfExcludedUntil;
        set {
            _selfExcludedUntil = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    public DateTime InsertedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}