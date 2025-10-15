using AppBackend.BusinessObjects.Data;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Extensions;

public static class DbConfig
{
    public static IServiceCollection AddDbConfig(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<HotelManagementContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        return services;
    }
}
