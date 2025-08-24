using System;

namespace Identity.Grains;

/// <summary>
/// Configuration options for AdminGrain behavior.
/// </summary>
public class UserManagementGrainOptions {
    /// <summary>
    /// How long to cache user data in memory before reloading from database.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);

    public string? FirebaseProjectId { get; set; }
    public string? FirebaseServiceAccountKeyPath { get; set; }
    public string? FirebaseServiceAccountJson { get; set; }
}
