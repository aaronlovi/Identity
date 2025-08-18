using System;

namespace Identity.Grains;

/// <summary>
/// Configuration options for AdminGrain behavior.
/// </summary>
public class AdminGrainOptions {
    /// <summary>
    /// How long to cache user data in memory before reloading from database.
    /// Default: 5 minutes.
    /// </summary>
    public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(5);
}
