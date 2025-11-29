using AppBackend.Services.ApiModels.ChatModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.AI;

public interface IGeminiKeyManager
{
    string GetAvailableKey();
    void MarkKeyAsExhausted(string apiKey);
    GeminiSettings GetSettings();
    int GetAvailableKeyCount();
}

/// <summary>
/// Manages multiple Gemini API keys with retry logic and quota management
/// </summary>
public class GeminiKeyManager : IGeminiKeyManager
{
    private readonly GeminiSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeminiKeyManager>? _logger;
    private readonly Random _random;
    
    private const string BLACKLIST_CACHE_KEY = "GeminiApiKey_Blacklist";
    private const string LAST_RESET_CACHE_KEY = "GeminiApiKey_LastReset";
    private static readonly TimeSpan RESET_TIME = new TimeSpan(7, 0, 0); // 7:00 AM
    
    public GeminiKeyManager(
        IConfiguration configuration, 
        IMemoryCache cache,
        ILogger<GeminiKeyManager>? logger = null)
    {
        _logger = logger;
        _cache = cache;
        _settings = new GeminiSettings();
        _random = new Random();
        
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
            
            // Initialize blacklist if needed
            InitializeBlacklist();
            
            // Check and reset blacklist if it's past 7 AM
            CheckAndResetBlacklist();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ FATAL ERROR in GeminiKeyManager constructor");
            throw;
        }
    }

    /// <summary>
    /// Get an available API key (not in blacklist)
    /// </summary>
    public string GetAvailableKey()
    {
        CheckAndResetBlacklist();
        
        var blacklist = GetBlacklist();
        var availableKeys = _settings.ApiKeys
            .Where(k => !blacklist.Contains(k))
            .ToList();
        
        if (availableKeys.Count == 0)
        {
            _logger?.LogWarning("⚠️ All API keys are exhausted! Resetting blacklist...");
            ResetBlacklist();
            availableKeys = _settings.ApiKeys.ToList();
        }
        
        var selectedKey = availableKeys[_random.Next(availableKeys.Count)];
        
        _logger?.LogDebug("Selected API key: {Prefix}... (Available: {Available}/{Total})", 
            selectedKey.Substring(0, Math.Min(15, selectedKey.Length)),
            availableKeys.Count,
            _settings.ApiKeys.Count);
        
        return selectedKey;
    }

    /// <summary>
    /// Mark an API key as exhausted (out of quota)
    /// </summary>
    public void MarkKeyAsExhausted(string apiKey)
    {
        var blacklist = GetBlacklist();
        
        if (!blacklist.Contains(apiKey))
        {
            blacklist.Add(apiKey);
            _cache.Set(BLACKLIST_CACHE_KEY, blacklist, TimeSpan.FromDays(1));
            
            _logger?.LogWarning("⚠️ API Key marked as exhausted: {Prefix}... ({Exhausted}/{Total} keys exhausted)",
                apiKey.Substring(0, Math.Min(15, apiKey.Length)),
                blacklist.Count,
                _settings.ApiKeys.Count);
        }
    }

    /// <summary>
    /// Get available key count
    /// </summary>
    public int GetAvailableKeyCount()
    {
        CheckAndResetBlacklist();
        var blacklist = GetBlacklist();
        return _settings.ApiKeys.Count - blacklist.Count;
    }

    /// <summary>
    /// Get Gemini settings
    /// </summary>
    public GeminiSettings GetSettings()
    {
        return _settings;
    }

    #region Private Helper Methods

    private void InitializeBlacklist()
    {
        if (!_cache.TryGetValue(BLACKLIST_CACHE_KEY, out HashSet<string> _))
        {
            _cache.Set(BLACKLIST_CACHE_KEY, new HashSet<string>(), TimeSpan.FromDays(1));
            _logger?.LogInformation("✅ Initialized empty blacklist cache");
        }
        
        if (!_cache.TryGetValue(LAST_RESET_CACHE_KEY, out DateTime _))
        {
            _cache.Set(LAST_RESET_CACHE_KEY, DateTime.Now, TimeSpan.FromDays(365));
            _logger?.LogInformation("✅ Initialized last reset timestamp");
        }
    }

    private HashSet<string> GetBlacklist()
    {
        if (_cache.TryGetValue(BLACKLIST_CACHE_KEY, out HashSet<string>? blacklist))
        {
            return blacklist ?? new HashSet<string>();
        }
        
        var newBlacklist = new HashSet<string>();
        _cache.Set(BLACKLIST_CACHE_KEY, newBlacklist, TimeSpan.FromDays(1));
        return newBlacklist;
    }

    private void CheckAndResetBlacklist()
    {
        var now = DateTime.Now;
        
        if (!_cache.TryGetValue(LAST_RESET_CACHE_KEY, out DateTime lastReset))
        {
            lastReset = now.Date;
            _cache.Set(LAST_RESET_CACHE_KEY, lastReset, TimeSpan.FromDays(365));
        }
        
        // Check if we've passed 7:00 AM today and haven't reset yet
        var todayReset = now.Date.Add(RESET_TIME);
        
        if (now >= todayReset && lastReset < todayReset)
        {
            _logger?.LogInformation("🔄 Auto-resetting API key blacklist at {Time} (Gemini quota refreshes daily at 7 AM)", now);
            ResetBlacklist();
            _cache.Set(LAST_RESET_CACHE_KEY, now, TimeSpan.FromDays(365));
        }
    }

    private void ResetBlacklist()
    {
        _cache.Set(BLACKLIST_CACHE_KEY, new HashSet<string>(), TimeSpan.FromDays(1));
        _logger?.LogInformation("✅ Blacklist reset successfully. All {Count} API keys are now available", _settings.ApiKeys.Count);
    }

    #endregion
}
