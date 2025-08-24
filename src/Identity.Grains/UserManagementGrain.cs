using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Protobuf.WellKnownTypes;
using Identity.Common.Mappers;
using Identity.GrainInterfaces;
using Identity.Infrastructure.Firebase;
using Identity.Infrastructure.Firebase.DomainModels;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.DTOs;
using Identity.Protos.V1;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Identity.Grains;

/// <summary>
/// Per-user Orleans grain implementation for admin operations.
/// </summary>
public class UserManagementGrain : Grain, IUserManagementGrain {
    private record SetUserStatusContext(UserStatus OldStatus, UserStatus NewStatus);
    private record UpdateRolesContext(List<string> AddRoles, List<string> RemoveRoles);

    private readonly long _userId;
    private UserDTO? _cachedUser;
    private readonly ILogger<UserManagementGrain> _logger;
    private readonly UserManagementGrainOptions _options;
    private readonly IIdentityDbmService _dbm;
    private readonly IEventPublisher _eventPublisher;

    private SetUserStatusContext? _setUserStatusContext;
    private UpdateRolesContext? _updateRolesContext;
    private CancellationToken? _ct;

    private SetUserStatusContext SetUserStatusCtx {
        get => _setUserStatusContext ?? throw new InvalidOperationException("SetUserStatusContext not set");
        set => _setUserStatusContext = value;
    }
    private UpdateRolesContext UpdateRolesCtx {
        get => _updateRolesContext ?? throw new InvalidOperationException("UpdateRolesContext not set");
        set => _updateRolesContext = value;
    }
    private CancellationToken Ct {
        get => _ct ?? CancellationToken.None;
        set => _ct = value;
    }

    public UserManagementGrain(
        ILogger<UserManagementGrain> logger,
        IOptions<UserManagementGrainOptions> options,
        IIdentityDbmService dbm,
        IEventPublisher eventPublisher) {
        _userId = this.GetPrimaryKeyLong();
        _logger = logger;
        _options = options.Value;
        _dbm = dbm;
        _eventPublisher = eventPublisher;
    }

    public override Task OnActivateAsync(CancellationToken ct) {
        _logger.LogInformation("AdminGrain activated for user_id: {UserId}. Cache expiry: {CacheExpiry}",
            _userId, _options.CacheExpiry);
        return base.OnActivateAsync(ct);
    }

    public async Task<GetUserResponse> GetUserAsync(CancellationToken ct = default) {
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", _userId);
        _logger.LogDebug("GetUserAsync");

        // Set context
        _ct = ct;

        Result res = await EnsureCachedUser();
        if (res.IsFailure) {
            return new GetUserResponse {
                ErrorInfo = Utils.CreateError(res.ErrorMessage)
            };
        }

        return new GetUserResponse {
            ErrorInfo = Utils.CreateSuccess(),
            User = UserMapper.UserDtoToDomain(_cachedUser!)
        };
    }

    public async Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, CancellationToken ct = default) {
        // Set logging context
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", _userId);
        using IDisposable? statusLogContext = _logger.BeginScope("NewStatus: {NewStatus}", newStatus);
        _logger.LogInformation("SetUserStatusAsync");

        // Set context
        SetUserStatusCtx = new(UserMapper.UserStatusDtoToDomain(_cachedUser?.Status), newStatus);
        Ct = ct;

        try {
            return await SetUserStatusCore(newStatus, ct);
        } catch (FirebaseAuthException fae) {
            _logger.LogWarning(fae, "SetFirebaseUserStatus failed with FirebaseAuthException: {Error}", fae.Message);
            return new SetUserStatusResponse {
                ErrorInfo = Utils.CreateError($"Failed to update Firebase user status - {fae.Message}")
            };
        } catch (OperationCanceledException oex) {
            _logger.LogWarning(oex, "Operation timed out: {Error}", oex.Message);
            return new SetUserStatusResponse {
                ErrorInfo = Utils.CreateError("Operation timed out")
            };
        } catch (Exception ex) {
            _logger.LogError(ex, "SetUserStatusAsync encountered an unexpected error: {Error}", ex.Message);
            return new SetUserStatusResponse {
                ErrorInfo = Utils.CreateError("An unexpected error occurred")
            };
        }
    }

    public async Task<UpdateUserRolesResponse> UpdateUserRolesAsync(List<string> addRoles, List<string> removeRoles, CancellationToken ct = default) {
        // Set logging context
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", this.GetPrimaryKeyLong());
        using IDisposable? addRolesLogContext = _logger.BeginScope("AddRoles: [{AddRoles}]", string.Join(",", addRoles));
        using IDisposable? removeRolesLogContext = _logger.BeginScope("RemoveRoles: [{RemoveRoles}]", string.Join(",", removeRoles));
        _logger.LogInformation("UpdateUserRolesAsync");

        // Set context
        UpdateRolesCtx = new UpdateRolesContext(addRoles, removeRoles);

        try {
            return await UpdateUserRolesCore(addRoles, removeRoles, ct);
        } catch (FirebaseAuthException fae) {
            _logger.LogWarning(fae, "SetFirebaseClaims failed with FirebaseAuthException: {Error}", fae.Message);
            return new UpdateUserRolesResponse {
                ErrorInfo = Utils.CreateError($"Failed to update Firebase user claims - {fae.Message}")
            };
        } catch (OperationCanceledException oex) {
            _logger.LogWarning(oex, "Operation timed out: {Error}", oex.Message);
            return new UpdateUserRolesResponse {
                ErrorInfo = Utils.CreateError("Operation timed out")
            };
        } catch (Exception ex) {
            _logger.LogError(ex, "UpdateUserRolesAsync encountered an unexpected error: {Error}", ex.Message);
            return new UpdateUserRolesResponse {
                ErrorInfo = Utils.CreateError("An unexpected error occurred")
            };
        }
    }

    public Task<MintCustomTokenResponse> MintCustomTokenAsync(int ttlMinutes = 15, IDictionary<string, string>? additionalClaims = null, CancellationToken cancellationToken = default) {
        _logger.LogInformation("MintCustomTokenAsync called for user_id: {UserId}, ttl: {TTL} minutes", _userId, ttlMinutes);

        // TODO: D-04 - Implement Firebase Admin SDK token minting
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);

        return Task.FromResult(new MintCustomTokenResponse {
            ErrorInfo = Utils.CreateSuccess(),
            CustomToken = $"stub_token_for_user_{_userId}",
            ExpiresAt = Timestamp.FromDateTime(expiresAt)
        });
    }

    #region PRIVATE HELPER METHODS

    private async Task<Result> EnsureCachedUser() {
        if (_cachedUser is not null) {
            _logger.LogDebug("EnsureCachedUser - already cached");
            return Result.Success;
        }

        _logger.LogDebug("EnsureCachedUser - fetching from DB");
        Result<UserDTO> res = await GetUncachedUser(Ct);
        if (res.IsSuccess)
            _cachedUser = res.Value!;
        
        return new Result(res.ErrorCode, res.ErrorMessage, res.ErrorParams);
    }

    private async Task<Result<UserDTO>> GetUncachedUser(CancellationToken ct) {
        Result<UserDTO> res = await _dbm.GetUser(_userId, ct);
        if (res.IsFailure)
            _logger.LogWarning("GetUserAsync failed with error: {Error}", res.ErrorMessage);
        return res;
    }

    private async Task<SetUserStatusResponse> SetUserStatusCore(UserStatus newStatus, CancellationToken ct) {
        Result res = await EnsureCachedUser().
            Then(UpdateUserStatus).
            Then(UpdateCachedUserStatus).
            Then(SetFirebaseClaims).
            Then(PublishUserStatusChangedEvent);

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

    private async Task<UpdateUserRolesResponse> UpdateUserRolesCore(List<string> addRoles, List<string> removeRoles, CancellationToken ct) {
        Result res = await EnsureCachedUser().
            Then(UpdateUserRoles).
            Then(UpdateCachedUserRoles).
            Then(SetFirebaseClaims).
            Then(PublishUserRolesUpdatedEvent);

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

    private async Task<Result> UpdateUserStatus() {
        _logger.LogInformation("UpdateUserStatus");

        string newUserStatusStr = UserMapper.UserStatusToDTO(SetUserStatusCtx.NewStatus);
        Result res = await _dbm.SetUserStatus(_userId, newUserStatusStr, Ct);
        if (res.IsFailure)
            _logger.LogWarning("UpdateUserStatus failed with error: {Error}", res.ErrorMessage);

        return res;
    }

    private Result UpdateCachedUserStatus() {
        if (_cachedUser is null)
            return Result.Failure(ErrorCodes.NotFound, $"UpdateCachedUserStatus - Cached user with id {_userId} not found");

        _cachedUser = _cachedUser with { Status = UserMapper.UserStatusToDTO(SetUserStatusCtx.NewStatus) };
        return Result.Success;
    }

    private Result UpdateCachedUserRoles() {
        if (_cachedUser is null)
            return Result.Failure(ErrorCodes.NotFound, $"UpdateCachedUserRoles - Cached user with id {_userId} not found");

        // Add new roles to the cached user
        foreach (string role in UpdateRolesCtx.AddRoles) {
            if (!_cachedUser.Roles.Contains(role))
                _cachedUser.Roles.Add(role);
        }

        // Remove roles from the cached user
        foreach (string role in UpdateRolesCtx.RemoveRoles) {
            if (!_cachedUser.Roles.Contains(role))
                continue;
            _ = _cachedUser.Roles.Remove(role);
        }

        return Result.Success;
    }

    private async Task<Result> UpdateUserRoles() {
        Result res = await _dbm.UpdateUserRoles(_userId, UpdateRolesCtx.AddRoles, UpdateRolesCtx.RemoveRoles, Ct);
        if (res.IsFailure)
            _logger.LogWarning("UpdateUserRoles failed with error: {Error}", res.ErrorMessage);
        return res;
    }

    private async Task<Result> SetFirebaseClaims() {
        // Skip Firebase operations if disabled (e.g., for testing)
        if (_options.DisableFirebaseOperations) {
            _logger.LogDebug("Firebase operations disabled, skipping SetCustomUserClaimsAsync");
            return Result.Success;
        }

        var claims = new Dictionary<string, object> {
            ["roles"] = _cachedUser!.Roles,
            ["status"] = _cachedUser.Status
        };

        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
            _cachedUser!.FirebaseUid, claims, Ct);
        
        return Result.Success;
    }

    private async Task<Result> PublishUserStatusChangedEvent() {
        try {
            var eventData = new UserStatusChangedEvent(
                UserId: _userId,
                PreviousStatus: UserMapper.UserStatusToDTO(SetUserStatusCtx.OldStatus),
                NewStatus: _cachedUser!.Status,
                ChangedBy: _options.DefaultChangedBy,
                ChangedAt: DateTime.UtcNow
            );

            await _eventPublisher.PublishAsync("UserStatusChanged", eventData, _userId, Ct);
            return Result.Success;
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to publish UserStatusChanged event for user {UserId}: {Error}", _userId, ex.Message);
            // Don't fail the entire operation if event publishing fails
            return Result.Success;
        }
    }

    private async Task<Result> PublishUserRolesUpdatedEvent() {
        try {
            var eventData = new UserRolesUpdatedEvent(
                UserId: _userId,
                AddedRoles: UpdateRolesCtx.AddRoles,
                RemovedRoles: UpdateRolesCtx.RemoveRoles,
                Roles: [.. _cachedUser!.Roles],
                ChangedBy: _options.DefaultChangedBy,
                ChangedAt: DateTime.UtcNow
            );

            await _eventPublisher.PublishAsync("UserRolesUpdated", eventData, _userId, Ct);
            return Result.Success;
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to publish UserRolesUpdated event for user {UserId}: {Error}", _userId, ex.Message);
            // Don't fail the entire operation if event publishing fails
            return Result.Success;
        }
    }

    #endregion
}
