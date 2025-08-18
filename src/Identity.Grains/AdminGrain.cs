using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Identity.Protos.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Identity.Grains;

/// <summary>
/// Per-user Orleans grain implementation for admin operations.
/// </summary>
public class AdminGrain : Grain, IAdminGrain {
    private readonly ILogger<AdminGrain> _logger;
    private readonly AdminGrainOptions _options;

    public AdminGrain(
        ILogger<AdminGrain> logger,
        IOptions<AdminGrainOptions> options) {
        _logger = logger;
        _options = options.Value;
    }

    public override Task OnActivateAsync(CancellationToken ct) {
        long userId = this.GetPrimaryKeyLong();
        _logger.LogInformation("AdminGrain activated for user_id: {UserId}. Cache expiry: {CacheExpiry}",
            userId, _options.CacheExpiry);
        return base.OnActivateAsync(ct);
    }

    public Task<GetUserResponse> GetUserAsync(CancellationToken cancellationToken = default) {
        long userId = this.GetPrimaryKeyLong();
        _logger.LogDebug("GetUserAsync called for user_id: {UserId}", userId);

        // TODO: D-03 - Replace with actual database lookup
        var stubUser = new User {
            UserId = userId,
            FirebaseUid = $"firebase_uid_{userId}",
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        stubUser.Roles.Add("player");

        return Task.FromResult(new GetUserResponse {
            ErrorInfo = new ErrorInfo { ErrorCode = AdminErrorCodes.Success },
            User = stubUser
        });
    }

    public Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, string? reason = null, CancellationToken cancellationToken = default) {
        long userId = this.GetPrimaryKeyLong();
        _logger.LogInformation("SetUserStatusAsync called for user_id: {UserId}, new status: {Status}, reason: {Reason}",
            userId, newStatus, reason);

        // TODO: D-04 - Implement actual status change with database and Firebase
        var stubUser = new User {
            UserId = userId,
            FirebaseUid = $"firebase_uid_{userId}",
            Status = newStatus, // Return the requested status as if it was set
            EmailVerified = true,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        stubUser.Roles.Add("player");

        return Task.FromResult(new SetUserStatusResponse {
            ErrorInfo = new ErrorInfo { ErrorCode = AdminErrorCodes.Success },
            User = stubUser
        });
    }

    public Task<UpdateUserRolesResponse> UpdateUserRolesAsync(IEnumerable<string> addRoles, IEnumerable<string> removeRoles, CancellationToken cancellationToken = default) {
        long userId = this.GetPrimaryKeyLong();
        _logger.LogInformation("UpdateUserRolesAsync called for user_id: {UserId}, add: [{AddRoles}], remove: [{RemoveRoles}]",
            userId, string.Join(",", addRoles), string.Join(",", removeRoles));

        // TODO: D-04 - Implement actual role updates with database and Firebase
        var stubUser = new User {
            UserId = userId,
            FirebaseUid = $"firebase_uid_{userId}",
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        // Simulate role changes for stub
        stubUser.Roles.Add("player");
        foreach (string role in addRoles) {
            stubUser.Roles.Add(role);
        }

        return Task.FromResult(new UpdateUserRolesResponse {
            ErrorInfo = new ErrorInfo { ErrorCode = AdminErrorCodes.Success },
            User = stubUser
        });
    }

    public Task<MintCustomTokenResponse> MintCustomTokenAsync(int ttlMinutes = 15, IDictionary<string, string>? additionalClaims = null, CancellationToken cancellationToken = default) {
        long userId = this.GetPrimaryKeyLong();
        _logger.LogInformation("MintCustomTokenAsync called for user_id: {UserId}, ttl: {TTL} minutes", userId, ttlMinutes);

        // TODO: D-04 - Implement Firebase Admin SDK token minting
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);

        return Task.FromResult(new MintCustomTokenResponse {
            ErrorInfo = new ErrorInfo { ErrorCode = AdminErrorCodes.Success },
            CustomToken = $"stub_token_for_user_{userId}",
            ExpiresAt = Timestamp.FromDateTime(expiresAt)
        });
    }
}
