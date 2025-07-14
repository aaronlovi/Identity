using Microsoft.Extensions.DependencyInjection;

namespace Identity.Domain;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddDomainServices(this IServiceCollection services) =>
        // Register domain services, e.g.:
        // services.AddScoped<IUserService, UserService>();
        services;
}
