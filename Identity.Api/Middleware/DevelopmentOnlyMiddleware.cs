using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Identity.Api.Middleware;

/// <summary>
/// Middleware that runs only in the Development environment to seed initial data.
/// </summary>
public class DevelopmentOnlyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DevelopmentOnlyMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public DevelopmentOnlyMiddleware(RequestDelegate next, ILogger<DevelopmentOnlyMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("Running development-only middleware to seed initial data.");

            // TODO: Add logic to seed the first super-admin or other development data.
            SeedDevelopmentData();
        }

        await _next(context);
    }

    private void SeedDevelopmentData() =>
        // Placeholder for seeding logic.
        _logger.LogInformation("Seeding development data (e.g., super-admin account).");
        // Add actual seeding logic here.
}