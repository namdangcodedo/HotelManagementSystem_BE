using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Generic;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services;
using AppBackend.Services.Authentication;
using AppBackend.Services.Helpers;
using AppBackend.Services.RateLimiting;
using AppBackend.Services.Services.AccountServices;
using AppBackend.Services.Services.AmenityServices;
using AppBackend.Services.Services.Email;
using AppBackend.Services.Services.EmployeeServices;
using AppBackend.Services.Services.CommonCodeServices;
using AppBackend.Services.Services.RoomServices;
using AppBackend.Services.Services.BookingServices;
using AppBackend.Services.Services.RoleServices;
using AppBackend.Services.MessageQueue;
using AppBackend.Services.Services.TransactionServices;

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
        services.AddScoped<IAmenityService, AmenityService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ICommonCodeService, CommonCodeService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IRoomAmenityService, RoomAmenityService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingManagementService, BookingManagementService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ITransactionService, TransactionService>();
        
        // Message Queue Service - Singleton for thread-safe queue
        services.AddSingleton<IBookingQueueService, BookingQueueService>();
        
        // Background Service for processing booking queue
        services.AddHostedService<BookingQueueProcessor>();
        
        services.AddSingleton<RateLimiterStore>();
        #endregion

        #region Helpers
        services.AddScoped<AccountHelper>();
        services.AddScoped<CacheHelper>();
        services.AddScoped<BookingTokenHelper>();
        services.AddScoped<AccountTokenHelper>();
        services.AddScoped<QRPaymentHelper>();
        #endregion

        return services;
    }
}