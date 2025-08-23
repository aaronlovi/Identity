using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Infrastructure.Persistence.DTOs;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbmInMemoryService : DbmInMemoryService, IIdentityDbmService, IDbmService {
    private readonly ILogger<IdentityDbmInMemoryService> _logger;

    private readonly IdentityDbmInMemoryData _data;

    public IdentityDbmInMemoryService(ILoggerFactory loggerFactory)
        : base(loggerFactory) {
        _logger = loggerFactory.CreateLogger<IdentityDbmInMemoryService>();
        _data = new();
        _logger.LogWarning("DbmInMemory is instantiated: persistence in RAM only");
    }

    public Task<Result<UserDTO>> GetUser(long userId, CancellationToken ct) {
        lock (Locker) {
            UserDTO? user = _data.GetUser(userId);

            if (user is null) {
                _logger.LogWarning("GetUser failed: User not found");
                return Task.FromResult(Result<UserDTO>.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }

            _logger.LogInformation("GetUser success");
            return Task.FromResult(Result<UserDTO>.Success(user));
        }
    }

    public Task<Result> SetUserStatus(long userId, string status, CancellationToken ct) {
        lock (Locker) {
            bool updated = _data.SetUserStatus(userId, status);
            if (!updated) {
                _logger.LogWarning("SetUserStatus failed: User not found");
                return Task.FromResult(Result.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }
            _logger.LogInformation("SetUserStatus success");
            return Task.FromResult(Result.Success);
        }
    }

    public Task<Result> UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove, CancellationToken ct) {
        lock (Locker) {
            bool updated = _data.UpdateUserRoles(userId, rolesToAdd, rolesToRemove);
            if (!updated) {
                _logger.LogWarning("UpdateUserRoles failed: User not found");
                return Task.FromResult(Result.Failure(ErrorCodes.NotFound, $"User with id '{userId}' not found"));
            }
            _logger.LogInformation("UpdateUserRoles success");
            return Task.FromResult(Result.Success);
        }
    }
}
