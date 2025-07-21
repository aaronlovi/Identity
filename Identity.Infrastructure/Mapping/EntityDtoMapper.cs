using System;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Database.DTOs;

namespace Identity.Infrastructure.Mapping;

/// <summary>
/// Provides manual mapping between domain entities and persistence-level DTOs.
/// </summary>
public static class EntityDtoMapper {
    /// <summary>
    /// Maps a User domain entity to a UserDto.
    /// </summary>
    public static UserDto ToDto(User user) {
        return new UserDto {
            Id = user.Id,
            Status = user.Status.ToString(),
            KycState = user.KycState,
            SelfExcludedUntil = user.SelfExcludedUntil,
            InsertedAt = user.InsertedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a UserDto to a User domain entity.
    /// </summary>
    public static User ToEntity(UserDto userDto) {
        return new User(
            id: userDto.Id,
            status: Enum.Parse<UserStatus>(userDto.Status),
            kycState: userDto.KycState,
            selfExcludedUntil: userDto.SelfExcludedUntil,
            insertedAt: userDto.InsertedAt,
            updatedAt: userDto.UpdatedAt
        );
    }

    /// <summary>
    /// Maps a Credential domain entity to a CredentialDto.
    /// </summary>
    public static CredentialDto ToDto(Credential credential) {
        return new CredentialDto {
            UserId = credential.UserId,
            Type = credential.Type.ToString(),
            ExternalId = credential.ExternalId,
            Hash = credential.Hash?.Hash,
            MfaSecret = credential.MfaSecret,
            InsertedAt = credential.InsertedAt,
            UpdatedAt = credential.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a CredentialDto to a Credential domain entity.
    /// </summary>
    public static Credential ToEntity(CredentialDto credentialDto) {
        return new Credential(
            userId: credentialDto.UserId,
            type: Enum.Parse<CredentialType>(credentialDto.Type),
            externalId: credentialDto.ExternalId,
            hash: credentialDto.Hash != null ? new PasswordHash(credentialDto.Hash) : null,
            mfaSecret: credentialDto.MfaSecret,
            insertedAt: credentialDto.InsertedAt,
            updatedAt: credentialDto.UpdatedAt
        );
    }
}