using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Infrastructure.Persistence.DTOs;
using InnoAndLogic.Shared;

namespace Identity.Infrastructure.Persistence;

public interface IIdentityDbmService {
    Task<Result<UserDTO>> GetUser(long userId, CancellationToken ct);
    Task<Result> SetUserStatus(long userId, string status, CancellationToken ct);
    Task<Result> UpdateUserRoles(long userId, IEnumerable<string> rolesToAdd, IEnumerable<string> rolesToRemove, CancellationToken ct);
}
