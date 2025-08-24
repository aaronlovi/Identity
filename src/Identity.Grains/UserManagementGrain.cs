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
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Identity.Grains;

/// <summary>
/// Per-user Orleans grain implementation for admin operations.
/// </summary>
public class UserManagementGrain : Grain, IUserManagementGrain {
    private record UpdateRolesContext(List<string> AddRoles, List<string> RemoveRoles);

    private readonly long _userId;
    private UserDTO? _cachedUser;
    private readonly ILogger<UserManagementGrain> _logger;
    private readonly UserManagementGrainOptions _options;
    private readonly IIdentityDbmService _dbm;

    private UserStatus? _newUserStatus;
    private UpdateRolesContext? _updateRolesContext;
    private CancellationToken? _ct;

    private UserStatus NewUserStatus {
        get => _newUserStatus ?? throw new InvalidOperationException("NewUserStatus not set");
        set => _newUserStatus = value;
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
            User = UserMapper.ToDomain(_cachedUser!)
        };
    }

    public async Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, CancellationToken ct = default) {
        using IDisposable? userLogContext = _logger.BeginScope("UserId: {UserId}", _userId);
        using IDisposable? statusLogContext = _logger.BeginScope("NewStatus: {NewStatus}", newStatus);
        _logger.LogInformation("SetUserStatusAsync");

        // Set context
        NewUserStatus = newStatus;
        Ct = ct;

        Result res = await EnsureCachedUser().
            Then(UpdateUserStatus).
            Then(UpdateCachedUserStatus);

        // TODO: D-04 - Implement actual status change with database and Firebase

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

        UpdateRolesCtx = new UpdateRolesContext(addRoles, removeRoles);

        Result res = await EnsureCachedUser().
            Then(UpdateUserRoles).
            Then(UpdateCachedUserRoles);

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

    private async Task<Result> UpdateUserStatus() {
        _logger.LogInformation("UpdateUserStatus");

        string newUserStatusStr = UserMapper.ToDTO(NewUserStatus);
        Result res = await _dbm.SetUserStatus(_userId, newUserStatusStr, Ct);
        if (res.IsFailure)
            _logger.LogWarning("UpdateUserStatus failed with error: {Error}", res.ErrorMessage);

        return res;
    }

    private Result UpdateCachedUserStatus() {
        if (_cachedUser is null)
            return Result.Failure(ErrorCodes.NotFound, $"UpdateCachedUserStatus - Cached user with id {_userId} not found");

        _cachedUser = _cachedUser with { Status = UserMapper.ToDTO(NewUserStatus) };
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
}
