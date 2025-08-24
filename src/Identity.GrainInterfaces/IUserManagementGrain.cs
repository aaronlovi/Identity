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
[Alias("UserManagementGrain")]
public interface IUserManagementGrain : IGrainWithIntegerKey {
    /// <summary>
    /// Get user information by user_id (grain key).
    /// Returns stub data for D-02.
    /// </summary>
    [Alias("GetUserById")]
    Task<GetUserResponse> GetUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user status - stub implementation for D-02.
    /// </summary>
    [Alias("SetUserStatus")]
    Task<SetUserStatusResponse> SetUserStatusAsync(UserStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user roles - stub implementation for D-02.
    /// </summary>
    [Alias("UpdateUserRoles")]
    Task<UpdateUserRolesResponse> UpdateUserRolesAsync(List<string> addRoles, List<string> removeRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mint a custom Firebase token - stub implementation for D-02.
    /// </summary>
    [Alias("MintCustomToken")]
    Task<MintCustomTokenResponse> MintCustomTokenAsync(int ttlMinutes = 15, IDictionary<string, string>? additionalClaims = null, CancellationToken cancellationToken = default);
}
