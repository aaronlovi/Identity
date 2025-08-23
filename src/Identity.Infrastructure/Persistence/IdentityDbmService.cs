using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Infrastructure.Persistence.DTOs;
using Identity.Infrastructure.Persistence.Statements;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbmService : DbmService, IIdentityDbmService {
    private readonly ILogger<IdentityDbmService> _logger;

    public IdentityDbmService(
        ILoggerFactory loggerFactory,
        PostgresExecutor exec,
        DatabaseOptions options,
        DbMigrations migrations) : base(loggerFactory, exec, options, migrations) {
        _logger = loggerFactory.CreateLogger<IdentityDbmService>();
    }

    public async Task<Result<UserDTO>> GetUser(long userId, CancellationToken ct) {
        using IDisposable? userIdLogContext = _logger.BeginScope("UserId: {UserId}", userId);

        var stmt = new GetUserStmt(userId);
        Result res = await Executor.ExecuteQueryWithRetry(stmt, ct);
        
        if (res.IsFailure) {
            _logger.LogWarning("GetUserAsync failed: {Error}", res.ErrorMessage);
            return Result<UserDTO>.Failure(res);
        }

        if (res.IsSuccess && stmt.User.IsEmpty) {
            _logger.LogInformation("GetUser - no such user");
            return Result<UserDTO>.Failure(ErrorCodes.NotFound, $"No such user with id {userId}");
        }

        _logger.LogInformation("GetUser - success");
        return Result<UserDTO>.Success(stmt.User);
    }

    public async Task<Result> SetUserStatus(long userId, string status, CancellationToken ct) {
        using IDisposable? userIdLogContext = _logger.BeginScope("UserId: {UserId}", userId);
        using IDisposable? statusLogContext = _logger.BeginScope("Status: {Status}", status);
        _logger.LogInformation("SetUserStatus");

        var stmt = new SetUserStatusStmt(userId, status);
        Result res = await Executor.ExecuteQueryWithRetry(stmt, ct);
        
        if (res.IsFailure) {
            _logger.LogWarning("SetUserStatus failed: {Error}", res.ErrorMessage);
            return Result.Failure(res);
        }
        if (res.IsSuccess && stmt.NumRowsAffected == 0) {
            _logger.LogInformation("SetUserStatus - no such user");
            return Result.Failure(ErrorCodes.NotFound, $"No such user with id {userId}");
        }
        _logger.LogInformation("SetUserStatus - success");
        return Result.Success;
    }

    public async Task<Result> UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove, CancellationToken ct) {
        using IDisposable? userIdLogContext = _logger.BeginScope("UserId: {UserId}", userId);
        using IDisposable? rolesToAddLogContext = _logger.BeginScope("RolesToAdd: {RolesToAdd}", string.Join(",", rolesToAdd));
        using IDisposable? rolesToRemoveLogContext = _logger.BeginScope("RolesToRemove: {RolesToRemove}", string.Join(",", rolesToRemove));
        _logger.LogInformation("UpdateUserRoles");

        var stmt = new UpdateUserRolesStmt(userId, rolesToAdd, rolesToRemove);
        Result res = await Executor.ExecuteQueryWithRetry(stmt, ct);
        
        if (res.IsFailure) {
            _logger.LogWarning("UpdateUserRoles failed: {Error}", res.ErrorMessage);
            return Result.Failure(res);
        }
        if (res.IsSuccess && stmt.NumRowsAffected == 0) {
            _logger.LogInformation("UpdateUserRoles - no such user or no roles changed");
            return Result.Failure(ErrorCodes.NotFound, $"No such user with id {userId} or no roles change");
        }
        _logger.LogInformation("UpdateUserRoles - success");
        return Result.Success;
    }
}
