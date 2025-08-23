using System;
using FluentAssertions;
using Identity.Common.Mappers;
using Identity.Infrastructure.Persistence.DTOs;
using Identity.Protos.V1;
using Xunit;

namespace Identity.Grains.Tests;

public sealed class UserMapperTests {
    
    #region ToDomain(UserDTO) Tests

    [Fact]
    public void ToDomain_WithValidUserDTO_ShouldMapAllFieldsCorrectly() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 12345,
            FirebaseUid: "firebase_abc123",
            Roles: ["player", "premium"],
            Status: "active",
            CreatedAt: new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            UpdatedAt: new DateTime(2023, 6, 20, 14, 45, 30, DateTimeKind.Utc)
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Should().NotBeNull();
        _ = user.UserId.Should().Be(12345);
        _ = user.FirebaseUid.Should().Be("firebase_abc123");
        _ = user.Status.Should().Be(UserStatus.Active);
        _ = user.Roles.Should().HaveCount(2);
        _ = user.Roles.Should().Contain("player");
        _ = user.Roles.Should().Contain("premium");
        _ = user.CreatedAt.Should().NotBeNull();
        _ = user.UpdatedAt.Should().NotBeNull();
        _ = user.EmailVerified.Should().BeTrue(); // Default value
    }

    [Theory]
    [InlineData("active", UserStatus.Active)]
    [InlineData("ACTIVE", UserStatus.Active)]
    [InlineData("Active", UserStatus.Active)]
    [InlineData("banned", UserStatus.Banned)]
    [InlineData("BANNED", UserStatus.Banned)]
    [InlineData("Banned", UserStatus.Banned)]
    [InlineData("shadow_banned", UserStatus.ShadowBanned)]
    [InlineData("SHADOW_BANNED", UserStatus.ShadowBanned)]
    [InlineData("Shadow_Banned", UserStatus.ShadowBanned)]
    public void ToDomain_WithDifferentStatusValues_ShouldMapCorrectly(string dtoStatus, UserStatus expectedStatus) {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: dtoStatus,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void ToDomain_WithUnknownStatus_ShouldDefaultToUnspecified() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "unknown_status",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Status.Should().Be(UserStatus.Unspecified);
    }

    [Fact]
    public void ToDomain_WithEmptyRoles_ShouldCreateEmptyRolesList() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: [],
            Status: "active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Roles.Should().NotBeNull();
        _ = user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_WithNullRoles_ShouldCreateEmptyRolesList() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: null!,
            Status: "active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Roles.Should().NotBeNull();
        _ = user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_WithMultipleRoles_ShouldPreserveAllRoles() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player", "admin", "moderator", "premium", "beta_tester"],
            Status: "active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Roles.Should().HaveCount(5);
        _ = user.Roles.Should().Contain("player");
        _ = user.Roles.Should().Contain("admin");
        _ = user.Roles.Should().Contain("moderator");
        _ = user.Roles.Should().Contain("premium");
        _ = user.Roles.Should().Contain("beta_tester");
    }

    [Fact]
    public void ToDomain_WithNullDTO_ShouldThrowArgumentNullException() {
        // Act & Assert
        Func<User> act = () => UserMapper.ToDomain(null!);
        _ = act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDTO(UserStatus) Tests

    [Theory]
    [InlineData(UserStatus.Active, "active")]
    [InlineData(UserStatus.Banned, "banned")]
    [InlineData(UserStatus.ShadowBanned, "shadow_banned")]
    [InlineData(UserStatus.Unspecified, "active")]
    public void ToDTO_WithValidUserStatus_ShouldMapCorrectly(UserStatus userStatus, string expectedDtoStatus) {
        // Act
        string dtoStatus = UserMapper.ToDTO(userStatus);

        // Assert
        _ = dtoStatus.Should().Be(expectedDtoStatus);
    }

    [Fact]
    public void ToDTO_WithInvalidUserStatus_ShouldReturnActive() {
        // Arrange - Use an invalid enum value
        var invalidStatus = (UserStatus)999;

        // Act
        string dtoStatus = UserMapper.ToDTO(invalidStatus);

        // Assert
        _ = dtoStatus.Should().Be("active"); // Should default to active
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void ToDomain_ToDTO_RoundTrip_ShouldPreserveStatus() {
        // Arrange
        UserStatus[] originalStatuses = [UserStatus.Active, UserStatus.Banned, UserStatus.ShadowBanned];

        foreach (UserStatus originalStatus in originalStatuses) {
            // Act - Convert to DTO and back to domain
            string dtoStatus = UserMapper.ToDTO(originalStatus);
            var userDto = new UserDTO(
                UserId: 1,
                FirebaseUid: "test",
                Roles: ["player"],
                Status: dtoStatus,
                CreatedAt: DateTime.UtcNow,
                UpdatedAt: DateTime.UtcNow
            );
            User domainUser = UserMapper.ToDomain(userDto);

            // Assert
            _ = domainUser.Status.Should().Be(originalStatus,
                $"Round-trip conversion should preserve {originalStatus}");
        }
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void ToDomain_WithNullFirebaseUid_ShouldHandleGracefully() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: null!,
            Roles: ["player"],
            Status: "active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Should().NotBeNull();
        _ = user.FirebaseUid.Should().BeEmpty(); // Should be converted to empty string
    }

    [Fact]
    public void ToDomain_WithEmptyFirebaseUid_ShouldHandleGracefully() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "",
            Roles: ["player"],
            Status: "active",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Should().NotBeNull();
        _ = user.FirebaseUid.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_WithNullStatus_ShouldDefaultToUnspecified() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: null!,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Status.Should().Be(UserStatus.Unspecified);
    }

    [Fact]
    public void ToDomain_WithEmptyStatus_ShouldDefaultToUnspecified() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Status.Should().Be(UserStatus.Unspecified);
    }

    [Fact]
    public void ToDomain_WithWhitespaceStatus_ShouldDefaultToUnspecified() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "   ",
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Status.Should().Be(UserStatus.Unspecified);
    }

    #endregion

    #region DateTime Mapping Tests

    [Fact]
    public void ToDomain_WithUtcDateTimes_ShouldMapTimestampsCorrectly() {
        // Arrange
        var createdAt = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2023, 6, 20, 14, 45, 30, DateTimeKind.Utc);
        
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "active",
            CreatedAt: createdAt,
            UpdatedAt: updatedAt
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.CreatedAt.Should().NotBeNull();
        _ = user.UpdatedAt.Should().NotBeNull();
        
        // Convert back to DateTime for comparison
        var mappedCreatedAt = user.CreatedAt.ToDateTime();
        var mappedUpdatedAt = user.UpdatedAt.ToDateTime();

        _ = mappedCreatedAt.Should().Be(createdAt);
        _ = mappedUpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void ToDomain_WithLocalDateTimes_ShouldConvertToUtc() {
        // Arrange
        var localCreatedAt = new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Local);
        var localUpdatedAt = new DateTime(2023, 6, 20, 14, 45, 30, DateTimeKind.Local);
        
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "active",
            CreatedAt: localCreatedAt,
            UpdatedAt: localUpdatedAt
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Should().NotBeNull();
        _ = user.CreatedAt.Should().NotBeNull();
        _ = user.UpdatedAt.Should().NotBeNull();
        
        // The mapper should convert to UTC
        var mappedCreatedAt = user.CreatedAt.ToDateTime();
        var mappedUpdatedAt = user.UpdatedAt.ToDateTime();

        _ = mappedCreatedAt.Should().Be(localCreatedAt.ToUniversalTime());
        _ = mappedUpdatedAt.Should().Be(localUpdatedAt.ToUniversalTime());
    }

    [Fact]
    public void ToDomain_WithMinDateTime_ShouldHandleGracefully() {
        // Arrange
        var userDto = new UserDTO(
            UserId: 1,
            FirebaseUid: "test_uid",
            Roles: ["player"],
            Status: "active",
            CreatedAt: DateTime.MinValue,
            UpdatedAt: DateTime.MinValue
        );

        // Act
        User user = UserMapper.ToDomain(userDto);

        // Assert
        _ = user.Should().NotBeNull();
        _ = user.CreatedAt.Should().NotBeNull();
        _ = user.UpdatedAt.Should().NotBeNull();
    }

    #endregion
}