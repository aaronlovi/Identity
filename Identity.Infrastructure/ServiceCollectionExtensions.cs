using System.Collections.Generic;
using System.Reflection;
using InnoAndLogic.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions {
    private const string DbmOptionsSectionName = "DbmOptions";

    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="cfg">The application configuration.</param>
    /// <param name="externalMigrationAssemblies">
    /// Optional list of assemblies containing migrations. The "Identity.Infrastructure" assembly is always included.
    /// </param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration cfg,
        List<Assembly>? externalMigrationAssemblies = null) {

        externalMigrationAssemblies = EnsureMigrationAssemblies(externalMigrationAssemblies);

        return DbmHostConfig.ConfigurePersistenceServices(
            services, cfg, DbmOptionsSectionName, externalMigrationAssemblies);
    }

    /// <summary>
    /// Ensures the external migration assemblies list is initialized and always contains the Identity.Infrastructure assembly.
    /// </summary>
    /// <param name="externalMigrationAssemblies">The list of external migration assemblies.</param>
    /// <returns>A list of assemblies containing the Identity.Infrastructure assembly.</returns>
    private static List<Assembly> EnsureMigrationAssemblies(List<Assembly>? externalMigrationAssemblies) {
        Assembly infrastructureAssembly = typeof(ServiceCollectionExtensions).Assembly;
        externalMigrationAssemblies ??= [];

        if (!externalMigrationAssemblies.Contains(infrastructureAssembly))
            externalMigrationAssemblies.Add(infrastructureAssembly);

        return externalMigrationAssemblies;
    }
}
