using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.Extensions;

public static class CorsConfig
{
    public static IServiceCollection AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });
        return services;
    }
}
