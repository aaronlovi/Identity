using System;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Database.DTOs;
using Identity.Infrastructure.Mapping;
using Xunit;

namespace Identity.Infrastructure.Tests;

/// <summary>
/// Unit tests for the EntityDtoMapper class.
/// </summary>
public class EntityDtoMapperTests {
    [Fact]
    public void User_ToDto_MapsCorrectly() {
        // Arrange
        var user = new User(
            id: 1,
            status: UserStatus.Active,
            kycState: "Verified",
            selfExcludedUntil: DateTime.UtcNow.AddDays(1),
            insertedAt: DateTime.UtcNow.AddDays(-1),
            updatedAt: DateTime.UtcNow
        );

        // Act
        UserDto userDto = EntityDtoMapper.ToDto(user);

        // Assert
        Assert.Equal(user.Id, userDto.Id);
        Assert.Equal(user.Status.ToString(), userDto.Status);
        Assert.Equal(user.KycState, userDto.KycState);
        Assert.Equal(user.SelfExcludedUntil, userDto.SelfExcludedUntil);
        Assert.Equal(user.InsertedAt, userDto.InsertedAt);
        Assert.Equal(user.UpdatedAt, userDto.UpdatedAt);
    }

    [Fact]
    public void User_ToEntity_MapsCorrectly() {
        // Arrange
        var userDto = new UserDto {
            Id = 1,
            Status = "Active",
            KycState = "Verified",
            SelfExcludedUntil = DateTime.UtcNow.AddDays(1),
            InsertedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        User user = EntityDtoMapper.ToEntity(userDto);

        // Assert
        Assert.Equal(userDto.Id, user.Id);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(userDto.KycState, user.KycState);
        Assert.Equal(userDto.SelfExcludedUntil, user.SelfExcludedUntil);
        Assert.Equal(userDto.InsertedAt, user.InsertedAt);
        Assert.Equal(userDto.UpdatedAt, user.UpdatedAt);
    }

    [Fact]
    public void Credential_ToDto_MapsCorrectly() {
        // Arrange
        var credential = new Credential(
            userId: 1,
            type: CredentialType.Password,
            externalId: "external-id",
            hash: new PasswordHash("hashed-password"),
            mfaSecret: "mfa-secret",
            insertedAt: DateTime.UtcNow.AddDays(-1),
            updatedAt: DateTime.UtcNow
        );

        // Act
        CredentialDto credentialDto = EntityDtoMapper.ToDto(credential);

        // Assert
        Assert.Equal(credential.UserId, credentialDto.UserId);
        Assert.Equal(credential.Type.ToString(), credentialDto.Type);
        Assert.Equal(credential.ExternalId, credentialDto.ExternalId);
        Assert.Equal(credential.Hash?.Hash, credentialDto.Hash);
        Assert.Equal(credential.MfaSecret, credentialDto.MfaSecret);
        Assert.Equal(credential.InsertedAt, credentialDto.InsertedAt);
        Assert.Equal(credential.UpdatedAt, credentialDto.UpdatedAt);
    }

    [Fact]
    public void Credential_ToEntity_MapsCorrectly() {
        // Arrange
        var credentialDto = new CredentialDto {
            UserId = 1,
            Type = "Password",
            ExternalId = "external-id",
            Hash = "hashed-password",
            MfaSecret = "mfa-secret",
            InsertedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        Credential credential = EntityDtoMapper.ToEntity(credentialDto);

        // Assert
        Assert.Equal(credentialDto.UserId, credential.UserId);
        Assert.Equal(CredentialType.Password, credential.Type);
        Assert.Equal(credentialDto.ExternalId, credential.ExternalId);
        Assert.Equal(credentialDto.Hash, credential.Hash?.Hash);
        Assert.Equal(credentialDto.MfaSecret, credential.MfaSecret);
        Assert.Equal(credentialDto.InsertedAt, credential.InsertedAt);
        Assert.Equal(credentialDto.UpdatedAt, credential.UpdatedAt);
    }
}