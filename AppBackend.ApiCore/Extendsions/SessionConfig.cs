using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.Extensions;

public static class SessionConfig
{
    public static IServiceCollection AddSessionConfig(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });
        return services;
    }
}
