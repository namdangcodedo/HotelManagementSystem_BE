using Microsoft.Extensions.Caching.Memory;
using System;

namespace AppBackend.Services.Helpers
{
    public enum CachePrefix
    {
        RefreshToken,
        AccessToken,
        UserSession,
        OtpCode // Added for OTP caching
    }

    public static class CachePrefixExtensions
    {
        public static string ToPrefix(this CachePrefix prefix)
        {
            return prefix switch
            {
                CachePrefix.RefreshToken => "refresh_token:",
                CachePrefix.AccessToken => "access_token:",
                CachePrefix.UserSession => "user_session:",
                CachePrefix.OtpCode => "otp_code:", // Added for OTP
                _ => "cache:"
            };
        }
    }

    public class CacheHelper
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _defaultRefreshTokenTTL = TimeSpan.FromDays(7); // Industry standard: 7 days
        private readonly TimeSpan _defaultAccessTokenTTL = TimeSpan.FromMinutes(30); // Example: 30 minutes
        private readonly TimeSpan _defaultUserSessionTTL = TimeSpan.FromHours(2); // Example: 2 hours

        public CacheHelper(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void Set<T>(CachePrefix prefix, string key, T value, TimeSpan? ttl = null)
        {
            var cacheKey = prefix.ToPrefix() + key;
            var absoluteExpiration = DateTimeOffset.UtcNow + (ttl ?? GetDefaultTTL(prefix));
            _cache.Set(cacheKey, value, absoluteExpiration);
        }

        public T? Get<T>(CachePrefix prefix, string key)
        {
            var cacheKey = prefix.ToPrefix() + key;
            return _cache.TryGetValue(cacheKey, out T value) ? value : default;
        }

        public void Remove(CachePrefix prefix, string key)
        {
            var cacheKey = prefix.ToPrefix() + key;
            _cache.Remove(cacheKey);
        }

        private TimeSpan GetDefaultTTL(CachePrefix prefix)
        {
            return prefix switch
            {
                CachePrefix.RefreshToken => _defaultRefreshTokenTTL,
                CachePrefix.AccessToken => _defaultAccessTokenTTL,
                CachePrefix.UserSession => _defaultUserSessionTTL,
                _ => TimeSpan.FromHours(1)
            };
        }
    }
}
