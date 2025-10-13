using AppBackend.Repositories.Generic;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.Email;
using AppBackend.Services.ServicesHelpers;

namespace AppBackend.Extensions;

public static class ServicesConfig
{
    public static IServiceCollection AddServicesConfig(this IServiceCollection services)
    {
        #region Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        #endregion

        #region Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        #endregion

        #region Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddSingleton<RateLimiterStore>();

        #endregion

        #region Helpers
        services.AddScoped<UserHelper>();
        #endregion

        return services;
    }
}