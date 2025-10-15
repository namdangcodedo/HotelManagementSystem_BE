using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Generic;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services;
using AppBackend.Services.AccountServices;
using AppBackend.Services.Authentication;
using AppBackend.Services.Helpers;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.Email;

namespace AppBackend.ApiCore.Extensions;

public static class ServicesConfig
{
    public static IServiceCollection AddServicesConfig(this IServiceCollection services)
    {
        #region Generic Repository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        #endregion

        #region UnitOfWork
        services.AddScoped<IUnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<HotelManagementContext>();
            return new UnitOfWork(context);
        });
        #endregion

        #region Services
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IGoogleLoginService, GoogleLoginService>();
        services.AddSingleton<RateLimiterStore>();
        #endregion

        #region Helpers
        services.AddScoped<AccountHelper>();
        #endregion

        return services;
    }
}