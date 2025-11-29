using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.ApiModels.ChatModel;
using AppBackend.Services.Services.AI;
using AppBackend.Services.Services.RoomServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IGeminiKeyManager, GeminiKeyManager>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddScoped<IChatService, ChatService>();

        // Register Hotel Booking Plugin (needs IRoomService from existing DI)
        services.AddScoped<HotelBookingPlugin>(sp =>
        {
            var roomService = sp.GetRequiredService<IRoomService>();
            return new HotelBookingPlugin(roomService);
        });

        return services;
    }
}

