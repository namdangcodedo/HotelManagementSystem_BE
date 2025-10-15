using AppBackend.BusinessObjects.AppSettings;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBackend.Extensions;

public static class CloudinaryConfig
{
    public static IServiceCollection AddCloudinaryConfig(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<CloudinarySettings>(
            config.GetSection("CloudinarySettings"));

        services.AddSingleton<Cloudinary>(sp =>
        {
            var settings = config.GetSection("CloudinarySettings").Get<CloudinarySettings>();
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            return new Cloudinary(account);
        });

        return services;
    }
}
