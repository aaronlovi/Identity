using System.Threading;
using System.Threading.Tasks;
using Identity.Domain.Entities;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;

namespace Identity.Infrastructure.Database;

/// <summary>
/// Interface for the Identity database management service.
/// </summary>
public interface IIdentityDbmService : IDbmService
{
    Task<Result> CreateUserAsync(User user, Credential credential, CancellationToken ct);
}
