using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Identity.Common.Mappers;
using Identity.GrainInterfaces;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.DTOs;
using Identity.Protos.V1;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Identity.Grains;

/// <summary>
/// Per-user Orleans grain implementation for admin operations.
/// </summary>
public class UserManagementGrain : Grain, IUserManagementGrain {
    private readonly long _userId;
    private readonly ILogger<UserManagementGrain> _logger;
    private readonly UserManagementGrainOptions _options;
    private readonly IIdentityDbmService _dbm;

    public UserManagementGrain(
        ILogger<UserManagementGrain> logger,
        IOptions<UserManagementGrainOptions> options,
        IIdentityDbmService dbm) {
        _userId = this.GetPrimaryKeyLong();
        _logger = logger;
        _options = options.Value;
        _dbm = dbm;
    }

    public override Task OnActivateAsync(CancellationToken ct) {
        _logger.LogInformation("AdminGrain activated for user_id: {UserId}. Cache expiry: {CacheExpiry}",
            _userId, _options.CacheExpiry);
        return base.OnActivateAsync(ct);
    }

    public async Task<GetUserResponse> GetUserAsync(CancellationToken ct = default) {
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", _userId);
        _logger.LogDebug("GetUserAsync");

        Result<UserDTO> res = await _dbm.GetUser(_userId, ct);
        if (res.IsFailure) {
            _logger.LogWarning("GetUserAsync failed with error: {Error}", res.ErrorMessage);
            return new GetUserResponse {
                ErrorInfo = Utils.CreateError(res.ErrorMessage)
            };
        }

        _logger.LogInformation("GetUserAsync success");
        User u = UserMapper.ToDomain(res.Value!);
        return new GetUserResponse {
            ErrorInfo = Utils.CreateSuccess(),
            User = UserMapper.ToDomain(res.Value!)
        };
    }

    public async Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, string? reason = null, CancellationToken ct = default) {
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", _userId);
        using IDisposable? statusLogContext = _logger.BeginScope("NewStatus: {NewStatus}", newStatus);
        _logger.LogInformation("SetUserStatusAsync");

        // TODO: D-04 - Implement actual status change with database and Firebase

        Result res = await _dbm.SetUserStatus(_userId, UserMapper.ToDTO(newStatus), ct);
        if (res.IsFailure) {
            _logger.LogWarning("SetUserStatusAsync failed with error: {Error}", res.ErrorMessage);
            return new SetUserStatusResponse {
                ErrorInfo = Utils.CreateError(res.ErrorMessage)
            };
        }

        _logger.LogInformation("SetUserStatusAsync success");
        return new SetUserStatusResponse {
            ErrorInfo = Utils.CreateSuccess()
        };
    }

    public async Task<UpdateUserRolesResponse> UpdateUserRolesAsync(
        List<string> addRoles,
        List<string> removeRoles,
        CancellationToken ct = default) {
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", this.GetPrimaryKeyLong());
        using IDisposable? addRolesLogContext = _logger.BeginScope("AddRoles: [{AddRoles}]", string.Join(",", addRoles));
        using IDisposable? removeRolesLogContext = _logger.BeginScope("RemoveRoles: [{RemoveRoles}]", string.Join(",", removeRoles));
        _logger.LogInformation("UpdateUserRolesAsync");

        Result res = await _dbm.UpdateUserRoles(_userId, addRoles, removeRoles, ct);
        if (res.IsFailure) {
            _logger.LogWarning("UpdateUserRolesAsync failed with error: {Error}", res.ErrorMessage);
            return new UpdateUserRolesResponse {
                ErrorInfo = Utils.CreateError(res.ErrorMessage)
            };
        }

        _logger.LogInformation("UpdateUserRolesAsync success");
        return new UpdateUserRolesResponse {
            ErrorInfo = Utils.CreateSuccess()
        };
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
