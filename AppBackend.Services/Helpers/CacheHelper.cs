using Microsoft.Extensions.Caching.Memory;
using System;

namespace AppBackend.Services.Helpers
{
    public enum CachePrefix
    {
        RefreshToken,
        AccessToken,
        UserSession,
        OtpCode, // Added for OTP caching
        RoomBookingLock, // Lock phòng khi đang được đặt
        BookingPayment // Thông tin booking chờ thanh toán
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
                CachePrefix.OtpCode => "otp_code:",
                CachePrefix.RoomBookingLock => "room_booking_lock:",
                CachePrefix.BookingPayment => "booking_payment:",
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
        private readonly TimeSpan _defaultRoomBookingLockTTL = TimeSpan.FromMinutes(10); // Lock phòng trong 10 phút
        private readonly TimeSpan _defaultBookingPaymentTTL = TimeSpan.FromMinutes(15); // Chờ thanh toán 15 phút

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

        /// <summary>
        /// Thử lock một resource với timeout
        /// </summary>
        public bool TryAcquireLock(CachePrefix prefix, string key, string lockValue, TimeSpan? ttl = null)
        {
            var cacheKey = prefix.ToPrefix() + key;
            var absoluteExpiration = DateTimeOffset.UtcNow + (ttl ?? GetDefaultTTL(prefix));
            
            // Chỉ set nếu key chưa tồn tại (atomic operation)
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                _cache.Set(cacheKey, lockValue, absoluteExpiration);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Release lock nếu lock value khớp
        /// </summary>
        public bool ReleaseLock(CachePrefix prefix, string key, string lockValue)
        {
            var cacheKey = prefix.ToPrefix() + key;
            if (_cache.TryGetValue(cacheKey, out string existingValue) && existingValue == lockValue)
            {
                _cache.Remove(cacheKey);
                return true;
            }
            return false;
        }

        private TimeSpan GetDefaultTTL(CachePrefix prefix)
        {
            return prefix switch
            {
                CachePrefix.RefreshToken => _defaultRefreshTokenTTL,
                CachePrefix.AccessToken => _defaultAccessTokenTTL,
                CachePrefix.UserSession => _defaultUserSessionTTL,
                CachePrefix.RoomBookingLock => _defaultRoomBookingLockTTL,
                CachePrefix.BookingPayment => _defaultBookingPaymentTTL,
                _ => TimeSpan.FromHours(1)
            };
        }
    }
}
