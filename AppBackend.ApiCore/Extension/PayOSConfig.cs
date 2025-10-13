using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.Helpers;
using Net.payOS;

namespace AppBackend.Extensions;

public static class PayOSAppConfig
{
    public static IServiceCollection AddPayOSConfig(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PayOSSettings>(config.GetSection("PayOS"));
        var payOSConfig = config.GetSection("PayOS").Get<PayOSSettings>();

        services.AddSingleton(new PayOS(payOSConfig.ClientId, payOSConfig.ApiKey, payOSConfig.ChecksumKey));
        services.AddSingleton(new PayOSHelper(payOSConfig));

        return services;
    }
}
