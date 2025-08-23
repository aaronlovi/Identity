using System;
using FluentAssertions;
using Xunit;

namespace Identity.Grains.Tests;

public sealed class UserManagementGrainOptionsTests {

    #region Default Values Tests

    [Fact]
    public void UserManagementGrainOptions_DefaultConstructor_ShouldHaveDefaultValues() {
        // Act
        var options = new UserManagementGrainOptions();

        // Assert
        _ = options.Should().NotBeNull();
        _ = options.CacheExpiry.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptValidValues() {
        // Arrange
        var options = new UserManagementGrainOptions();
        var expectedExpiry = TimeSpan.FromMinutes(10);

        // Act
        options.CacheExpiry = expectedExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(expectedExpiry);
    }

    [Theory]
    [InlineData(1)] // 1 minute
    [InlineData(5)] // 5 minutes (default)
    [InlineData(15)] // 15 minutes
    [InlineData(30)] // 30 minutes
    [InlineData(60)] // 1 hour
    [InlineData(120)] // 2 hours
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptVariousMinuteValues(int minutes) {
        // Arrange
        var options = new UserManagementGrainOptions();
        var expectedExpiry = TimeSpan.FromMinutes(minutes);

        // Act
        options.CacheExpiry = expectedExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(expectedExpiry);
        _ = options.CacheExpiry.TotalMinutes.Should().Be(minutes);
    }

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptZeroValue() {
        // Arrange
        var options = new UserManagementGrainOptions();
        TimeSpan zeroExpiry = TimeSpan.Zero;

        // Act
        options.CacheExpiry = zeroExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(zeroExpiry);
    }

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptNegativeValue() {
        // Arrange
        var options = new UserManagementGrainOptions();
        var negativeExpiry = TimeSpan.FromMinutes(-5);

        // Act
        options.CacheExpiry = negativeExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(negativeExpiry);
        _ = options.CacheExpiry.TotalMinutes.Should().Be(-5);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptMaxValue() {
        // Arrange
        var options = new UserManagementGrainOptions();
        TimeSpan maxExpiry = TimeSpan.MaxValue;

        // Act
        options.CacheExpiry = maxExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(maxExpiry);
    }

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptMinValue() {
        // Arrange
        var options = new UserManagementGrainOptions();
        TimeSpan minExpiry = TimeSpan.MinValue;

        // Act
        options.CacheExpiry = minExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(minExpiry);
    }

    [Fact]
    public void UserManagementGrainOptions_CacheExpiry_ShouldAcceptPreciseTimeSpan() {
        // Arrange
        var options = new UserManagementGrainOptions();
        TimeSpan preciseExpiry = new(0, 7, 33, 45, 123); // 7 hours, 33 minutes, 45 seconds, 123 milliseconds

        // Act
        options.CacheExpiry = preciseExpiry;

        // Assert
        _ = options.CacheExpiry.Should().Be(preciseExpiry);
        _ = options.CacheExpiry.Hours.Should().Be(7);
        _ = options.CacheExpiry.Minutes.Should().Be(33);
        _ = options.CacheExpiry.Seconds.Should().Be(45);
        _ = options.CacheExpiry.Milliseconds.Should().Be(123);
    }

    #endregion

    #region Multiple Instance Tests

    [Fact]
    public void UserManagementGrainOptions_MultipleInstances_ShouldBeIndependent() {
        // Arrange
        var options1 = new UserManagementGrainOptions();
        var options2 = new UserManagementGrainOptions();
        
        var expiry1 = TimeSpan.FromMinutes(10);
        var expiry2 = TimeSpan.FromMinutes(20);

        // Act
        options1.CacheExpiry = expiry1;
        options2.CacheExpiry = expiry2;

        // Assert
        _ = options1.CacheExpiry.Should().Be(expiry1);
        _ = options2.CacheExpiry.Should().Be(expiry2);
        _ = options1.CacheExpiry.Should().NotBe(options2.CacheExpiry);
    }

    [Fact]
    public void UserManagementGrainOptions_DefaultInstance_ShouldNotAffectOtherInstances() {
        // Arrange
        var defaultOptions = new UserManagementGrainOptions();
        var customOptions = new UserManagementGrainOptions();
        var customExpiry = TimeSpan.FromMinutes(15);

        // Act
        customOptions.CacheExpiry = customExpiry;

        // Assert
        _ = defaultOptions.CacheExpiry.Should().Be(TimeSpan.FromMinutes(5)); // Should remain default
        _ = customOptions.CacheExpiry.Should().Be(customExpiry);
    }

    #endregion

    #region Configuration Scenario Tests

    [Fact]
    public void UserManagementGrainOptions_FromConfiguration_ShouldSupportTypicalScenarios() {
        // This test simulates how options might be configured in production
        
        // Scenario 1: Development environment (shorter cache for faster feedback)
        var devOptions = new UserManagementGrainOptions {
            CacheExpiry = TimeSpan.FromMinutes(1)
        };

        // Scenario 2: Production environment (longer cache for performance)
        var prodOptions = new UserManagementGrainOptions {
            CacheExpiry = TimeSpan.FromMinutes(30)
        };

        // Scenario 3: Testing environment (no cache)
        var testOptions = new UserManagementGrainOptions {
            CacheExpiry = TimeSpan.Zero
        };

        // Assert
        _ = devOptions.CacheExpiry.Should().Be(TimeSpan.FromMinutes(1));
        _ = prodOptions.CacheExpiry.Should().Be(TimeSpan.FromMinutes(30));
        _ = testOptions.CacheExpiry.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData("00:01:00", 1)] // 1 minute
    [InlineData("00:05:00", 5)] // 5 minutes
    [InlineData("00:15:00", 15)] // 15 minutes
    [InlineData("01:00:00", 60)] // 1 hour
    public void UserManagementGrainOptions_TimeSpanParsing_ShouldSupportCommonFormats(string timeSpanString, int expectedMinutes) {
        // This test verifies that common TimeSpan formats work as expected
        // This would be useful when reading from configuration files
        
        // Arrange
        var options = new UserManagementGrainOptions();
        var parsedTimeSpan = TimeSpan.Parse(timeSpanString);

        // Act
        options.CacheExpiry = parsedTimeSpan;

        // Assert
        _ = options.CacheExpiry.TotalMinutes.Should().Be(expectedMinutes);
    }

    #endregion

    #region Validation Scenarios Tests

    [Fact]
    public void UserManagementGrainOptions_ReasonableCacheExpiry_ShouldBeWithinExpectedRange() {
        // This test documents reasonable values for cache expiry
        var options = new UserManagementGrainOptions();

        // Act - Test various reasonable values
        TimeSpan[] reasonableValues = [
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5), // Default
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(2)
        ];

        // Assert - All should be assignable without issues
        foreach (TimeSpan value in reasonableValues) {
            options.CacheExpiry = value;
            _ = options.CacheExpiry.Should().Be(value);
        }
    }

    [Fact]
    public void UserManagementGrainOptions_ExtremeCacheExpiry_ShouldStillWork() {
        // This test verifies the options class can handle extreme values
        // even if they might not be practical in real usage
        var options = new UserManagementGrainOptions();

        TimeSpan[] extremeValues = [
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromDays(365), // 1 year
            TimeSpan.FromDays(36500) // ~100 years
        ];

        foreach (TimeSpan value in extremeValues) {
            options.CacheExpiry = value;
            _ = options.CacheExpiry.Should().Be(value);
        }
    }

    #endregion
}