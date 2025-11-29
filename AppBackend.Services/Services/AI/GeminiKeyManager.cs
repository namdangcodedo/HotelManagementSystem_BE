using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<GeminiKeyManager>? _logger;

    public GeminiKeyManager(IConfiguration configuration, ILogger<GeminiKeyManager>? logger = null)
    {
        _logger = logger;
        _settings = new GeminiSettings();
        
        try
        {
            configuration.GetSection("GeminiSettings").Bind(_settings);
            
            _logger?.LogInformation("=== GeminiKeyManager Initialization ===");
            _logger?.LogInformation("Reading GeminiSettings from configuration...");
            
            // Validate API keys
            if (_settings.ApiKeys == null || _settings.ApiKeys.Count == 0)
            {
                _logger?.LogError("❌ No Gemini API keys configured in appsettings.json");
                throw new InvalidOperationException("No Gemini API keys configured in appsettings.json");
            }

            _logger?.LogInformation("✅ GeminiKeyManager initialized with {Count} API keys", _settings.ApiKeys.Count);
            _logger?.LogInformation("✅ Model ID: {ModelId}", _settings.ModelId);
            _logger?.LogInformation("✅ Max Tokens: {MaxTokens}", _settings.MaxTokens);
            _logger?.LogInformation("✅ Temperature: {Temperature}", _settings.Temperature);
            
            // Log first few chars of each key for verification
            for (int i = 0; i < _settings.ApiKeys.Count; i++)
            {
                var key = _settings.ApiKeys[i];
                _logger?.LogInformation("API Key {Index}: {Prefix}... (length: {Length})", 
                    i + 1, 
                    key.Substring(0, Math.Min(15, key.Length)),
                    key.Length);
            }
            
            _random = new Random();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ FATAL ERROR in GeminiKeyManager constructor");
            _logger?.LogError("Exception Type: {Type}", ex.GetType().Name);
            _logger?.LogError("Exception Message: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get a random API key for load balancing across multiple keys
    /// </summary>
    public string GetRandomKey()
    {
        var index = _random.Next(_settings.ApiKeys.Count);
        var key = _settings.ApiKeys[index];
        
        _logger?.LogDebug("Selected API key index: {Index} (total: {Total})", index, _settings.ApiKeys.Count);
        
        return key;
    }

    /// <summary>
    /// Get Gemini settings
    /// </summary>
    public GeminiSettings GetSettings()
    {
        return _settings;
    }
}
