using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services) =>
        // Register domain services, e.g.:
        // services.AddScoped<IUserService, UserService>();
        services;
}
