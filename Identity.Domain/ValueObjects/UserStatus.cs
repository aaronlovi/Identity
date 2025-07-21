namespace Identity.Domain.ValueObjects;

/// <summary>
/// Represents the status of a user.
/// </summary>
public enum UserStatus {
    Invalid = 0,
    Active,
    Banned,
    ShadowBanned
}