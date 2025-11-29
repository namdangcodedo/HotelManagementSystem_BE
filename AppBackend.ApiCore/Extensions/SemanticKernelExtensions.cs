using AppBackend.BusinessObjects.AppSettings;
using AppBackend.Services.ApiModels.ChatModel;
using AppBackend.Services.Services.AI;
using AppBackend.Services.Services.RoomServices;
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
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║  REGISTERING SEMANTIC KERNEL SERVICES    ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        
        // Register Gemini Settings
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        Console.WriteLine("✅ Configured GeminiSettings from appsettings.json");

        // Register AI Services
        services.AddSingleton<IGeminiKeyManager, GeminiKeyManager>();
        Console.WriteLine("✅ Registered IGeminiKeyManager as Singleton");
        
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        Console.WriteLine("✅ Registered IChatHistoryService as Scoped");
        
        services.AddScoped<IChatService, ChatService>();
        Console.WriteLine("✅ Registered IChatService as Scoped");

        // Register Hotel Booking Plugin (needs IRoomService from existing DI)
        services.AddScoped<HotelBookingPlugin>(sp =>
        {
            var roomService = sp.GetRequiredService<IRoomService>();
            var logger = sp.GetRequiredService<ILogger<HotelBookingPlugin>>();
            return new HotelBookingPlugin(roomService, logger);
        });
        Console.WriteLine("✅ Registered HotelBookingPlugin as Scoped with Logger");
        
        Console.WriteLine("✅ All Semantic Kernel services registered successfully");
        Console.WriteLine("");

        return services;
    }
}
