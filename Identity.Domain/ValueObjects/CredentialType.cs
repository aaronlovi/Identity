namespace Identity.Domain.ValueObjects;

/// <summary>
/// Represents the type of a credential.
/// </summary>
public enum CredentialType
{
    Invalid = 0,
    Password,
    OAuth
}