using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.ApiModels.ChatModel;
using AppBackend.Services.Services.AI;
using AppBackend.Services.Services.RoomServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppBackend.ApiCore.Extensions;

public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernelServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        // Register Gemini Settings
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        // Register AI Services
        services.AddSingleton<IGeminiKeyManager>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<GeminiKeyManager>>();
            return new GeminiKeyManager(configuration, cache, logger);
        });
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IChatService, ChatService>();

        // Register Hotel Booking Plugin (needs IRoomService from existing DI)
        services.AddScoped<HotelBookingPlugin>(sp =>
        {
            var roomService = sp.GetRequiredService<IRoomService>();
            var logger = sp.GetRequiredService<ILogger<HotelBookingPlugin>>();
            return new HotelBookingPlugin(roomService, logger);
        });
        return services;
    }
}
