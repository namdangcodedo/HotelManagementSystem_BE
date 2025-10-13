using AppBackend.Services.Mappers;

namespace AppBackend.Extensions;

public static class AutoMapperConfig
{
    public static IServiceCollection AddAutoMapperConfig(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}
