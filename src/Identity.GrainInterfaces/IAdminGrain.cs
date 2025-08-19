using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Protos.V1;
using Orleans;

namespace Identity.GrainInterfaces;

/// <summary>
/// Per-user Orleans grain for admin operations on identity data.
/// Grain key is the user_id (int64).
/// </summary>
public interface IAdminGrain : IGrainWithIntegerKey {
    /// <summary>
    /// Get user information by user_id (grain key).
    /// Returns stub data for D-02.
    /// </summary>
    Task<GetUserResponse> GetUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user status - stub implementation for D-02.
    /// </summary>
    Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user roles - stub implementation for D-02.
    /// </summary>
    Task<UpdateUserRolesResponse> UpdateUserRolesAsync(IEnumerable<string> addRoles, IEnumerable<string> removeRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mint a custom Firebase token - stub implementation for D-02.
    /// </summary>
    Task<MintCustomTokenResponse> MintCustomTokenAsync(int ttlMinutes = 15, IDictionary<string, string>? additionalClaims = null, CancellationToken cancellationToken = default);
}
