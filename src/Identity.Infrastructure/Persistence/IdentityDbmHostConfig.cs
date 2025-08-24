using System;
using System.Collections.Generic;
using System.Reflection;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Provides configuration methods for setting up database persistence services specific to the Identity context.
/// </summary>
public static class IdentityDbmHostConfig {
    /// <summary>
    /// Configures services for the IdentityDbmService or IdentityDbmInMemoryService based on the database provider specified in <see cref="DatabaseOptions"/>.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The IConfiguration instance to bind options from.</param>
    /// <param name="sectionName">The name of the configuration section for DatabaseOptions.</param>
    /// <param name="externalMigrationAssemblies">
    /// An optional collection of assemblies containing additional embedded migration scripts.
    /// If not provided, only the default assembly is used.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> with the configured services.</returns>
    public static IServiceCollection ConfigureIdentityPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        IEnumerable<Assembly>? externalMigrationAssemblies = null) {

        // Configure DatabaseOptions from configuration section
        _ = services.Configure<DatabaseOptions>(options => {
            IConfigurationSection section = configuration.GetSection(sectionName);
            section.Bind(options);
        });

        // Get the options to determine the provider
        var databaseOptions = new DatabaseOptions();
        IConfigurationSection configSection = configuration.GetSection(sectionName);
        configSection.Bind(databaseOptions);

        return databaseOptions.Provider switch {
            DatabaseProvider.InMemory => ConfigureInMemoryServices(services),
            DatabaseProvider.Postgres => ConfigurePostgresServices(services, externalMigrationAssemblies),
            _ => throw new InvalidOperationException($"Unsupported database provider: {databaseOptions.Provider}")
        };
    }

    private static IServiceCollection ConfigureInMemoryServices(IServiceCollection services) {
        return services.AddSingleton<IIdentityDbmService, IdentityDbmInMemoryService>(provider => {
            ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return new IdentityDbmInMemoryService(loggerFactory);
        });
    }

    private static IServiceCollection ConfigurePostgresServices(
        IServiceCollection services,
        IEnumerable<Assembly>? externalMigrationAssemblies) {
        return services.
            AddSingleton<PostgresExecutor>().
            AddSingleton<DbMigrations>(provider => new DbMigrations(
                provider.GetRequiredService<ILoggerFactory>(),
                provider.GetRequiredService<IOptions<DatabaseOptions>>(),
                externalMigrationAssemblies)).
            AddSingleton<IIdentityDbmService, IdentityDbmService>(provider => {
                ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                PostgresExecutor executor = provider.GetRequiredService<PostgresExecutor>();
                DbMigrations migrations = provider.GetRequiredService<DbMigrations>();
                DatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                return new IdentityDbmService(loggerFactory, executor, databaseOptions, migrations);
            });
    }
}
