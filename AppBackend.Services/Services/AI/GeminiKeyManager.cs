using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.Extensions.Configuration;

namespace AppBackend.Services.Services.AI;

public interface IGeminiKeyManager
{
    string GetRandomKey();
    GeminiSettings GetSettings();
}

/// <summary>
/// Manages multiple Gemini API keys for load balancing
/// </summary>
public class GeminiKeyManager : IGeminiKeyManager
{
    private readonly GeminiSettings _settings;
    private readonly Random _random;

    public GeminiKeyManager(IConfiguration configuration)
    {
        _settings = new GeminiSettings();
        configuration.GetSection("GeminiSettings").Bind(_settings);
        
        // Validate API keys
        if (_settings.ApiKeys == null || _settings.ApiKeys.Count == 0)
        {
            throw new InvalidOperationException("No Gemini API keys configured in appsettings.json");
        }

        _random = new Random();
    }

    /// <summary>
    /// Get a random API key for load balancing across multiple keys
    /// </summary>
    public string GetRandomKey()
    {
        var index = _random.Next(_settings.ApiKeys.Count);
        return _settings.ApiKeys[index];
    }

    /// <summary>
    /// Get Gemini settings
    /// </summary>
    public GeminiSettings GetSettings()
    {
        return _settings;
    }
}

