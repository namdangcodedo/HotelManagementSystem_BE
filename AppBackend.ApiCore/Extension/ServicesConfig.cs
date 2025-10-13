using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.Email;
using AppBackend.Services.Services.User;
using AppBackend.Services.ServicesHelpers;

namespace AppBackend.ApiCore.Extendsions;

public static class ServicesConfig
{
    public static IServiceCollection AddServicesConfig(this IServiceCollection services)
    {
        #region Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        #endregion

        #region Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        #endregion

        #region UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<HotelManagementDbContext>();
            var userRepo = provider.GetRequiredService<IUserRepository>();
            var roleRepo = provider.GetRequiredService<IRoleRepository>();
            return new UnitOfWork(context, userRepo, roleRepo);
        });
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