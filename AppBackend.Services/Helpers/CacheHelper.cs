using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AppBackend.Services.Helpers
{
    public enum CachePrefix
    {
        RefreshToken,
        AccessToken,
        UserSession,
        OtpCode, 
        RoomBookingLock, 
        BookingPayment 
        , RoomTypeInventory
        , AccountActivation 
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
                CachePrefix.RoomTypeInventory => "room_type_inventory:",
                CachePrefix.AccountActivation => "account_activation:", // ✅ Mapping prefix
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
        /// NOTE: Sử dụng khi cần lock phòng trong quá trình booking để tránh tranh chấp
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
        /// NOTE: Chỉ release lock nếu lockValue khớp để đảm bảo chỉ người tạo lock mới release được
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

        /// <summary>
        /// Giảm số lượng phòng available theo loại trong cache (dùng khi lock phòng)
        /// NOTE: Tránh trường hợp nhiều người cùng đặt và vượt quá số lượng phòng available
        /// </summary>
        public bool DecrementRoomTypeInventory(int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int quantity)
        {
            var key = $"{roomTypeId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            var cacheKey = CachePrefix.RoomTypeInventory.ToPrefix() + key;
            
            if (_cache.TryGetValue(cacheKey, out int currentInventory))
            {
                if (currentInventory >= quantity)
                {
                    _cache.Set(cacheKey, currentInventory - quantity, DateTimeOffset.UtcNow.AddMinutes(15));
                    return true;
                }
                return false; // Không đủ phòng
            }
            
            // Chưa có trong cache, khởi tạo giá trị âm để tracking
            // Service layer cần set giá trị ban đầu
            return false;
        }

        /// <summary>
        /// Tăng số lượng phòng available theo loại trong cache (dùng khi release lock)
        /// </summary>
        public void IncrementRoomTypeInventory(int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int quantity)
        {
            var key = $"{roomTypeId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            var cacheKey = CachePrefix.RoomTypeInventory.ToPrefix() + key;
            
            if (_cache.TryGetValue(cacheKey, out int currentInventory))
            {
                _cache.Set(cacheKey, currentInventory + quantity, DateTimeOffset.UtcNow.AddMinutes(15));
            }
        }

        /// <summary>
        /// Khởi tạo inventory cho room type trong khoảng thời gian
        /// NOTE: Gọi method này trước khi bắt đầu quá trình booking
        /// </summary>
        public void InitializeRoomTypeInventory(int roomTypeId, DateTime checkInDate, DateTime checkOutDate, int availableQuantity)
        {
            var key = $"{roomTypeId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            Set(CachePrefix.RoomTypeInventory, key, availableQuantity, TimeSpan.FromMinutes(15));
        }

        /// <summary>
        /// Lấy thông tin inventory hiện tại của room type
        /// </summary>
        public int? GetRoomTypeInventory(int roomTypeId, DateTime checkInDate, DateTime checkOutDate)
        {
            var key = $"{roomTypeId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            return Get<int?>(CachePrefix.RoomTypeInventory, key);
        }

        /// <summary>
        /// Xóa toàn bộ locks của một booking khi thanh toán thành công hoặc hủy
        /// </summary>
        public void ReleaseAllBookingLocks(List<int> roomIds, DateTime checkInDate, DateTime checkOutDate, string lockId)
        {
            foreach (var roomId in roomIds)
            {
                var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
                ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
            }
        }

        /// <summary>
        /// Set cache với custom key (không dùng prefix enum)
        /// </summary>
        public void SetCustom<T>(string fullKey, T value, TimeSpan? ttl = null)
        {
            var absoluteExpiration = DateTimeOffset.UtcNow + (ttl ?? TimeSpan.FromHours(1));
            _cache.Set(fullKey, value, absoluteExpiration);
        }

        /// <summary>
        /// Get cache với custom key (không dùng prefix enum)
        /// </summary>
        public T? GetCustom<T>(string fullKey)
        {
            return _cache.TryGetValue(fullKey, out T value) ? value : default;
        }

        /// <summary>
        /// Remove cache với custom key (không dùng prefix enum)
        /// </summary>
        public void RemoveCustom(string fullKey)
        {
            _cache.Remove(fullKey);
        }

        /// <summary>
        /// Search cache entries theo prefix
        /// </summary>
        public List<CacheEntry> SearchByPrefix(CachePrefix? prefix = null)
        {
            var entries = GetAllCacheEntries();
            
            if (prefix.HasValue)
            {
                var prefixString = prefix.Value.ToPrefix();
                return entries.Where(e => e.Key.StartsWith(prefixString)).ToList();
            }
            
            return entries;
        }

        /// <summary>
        /// Search cache entries theo pattern (contains)
        /// </summary>
        public List<CacheEntry> SearchByPattern(string pattern)
        {
            var entries = GetAllCacheEntries();
            return entries.Where(e => e.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Get all cache entries với thông tin chi tiết
        /// </summary>
        public List<CacheEntry> GetAllCacheEntries()
        {
            var result = new List<CacheEntry>();
            
            // Sử dụng reflection để truy cập internal cache entries
            var coherentState = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
            if (coherentState == null) return result;

            var coherentStateValue = coherentState.GetValue(_cache);
            if (coherentStateValue == null) return result;

            var entries = coherentStateValue.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            if (entries == null) return result;

            var entriesCollection = entries.GetValue(coherentStateValue) as ICollection;
            if (entriesCollection == null) return result;

            foreach (var item in entriesCollection)
            {
                var keyProperty = item.GetType().GetProperty("Key");
                var valueProperty = item.GetType().GetProperty("Value");
                var expirationProperty = item.GetType().GetProperty("AbsoluteExpiration");
                
                if (keyProperty != null && valueProperty != null)
                {
                    var key = keyProperty.GetValue(item)?.ToString() ?? "";
                    var value = valueProperty.GetValue(item);
                    var expiration = expirationProperty?.GetValue(item) as DateTimeOffset?;
                    
                    result.Add(new CacheEntry
                    {
                        Key = key,
                        Value = value,
                        ExpiresAt = expiration,
                        Type = value?.GetType().Name ?? "null"
                    });
                }
            }
            
            return result;
        }

        /// <summary>
        /// Get cache statistics by prefix
        /// </summary>
        public Dictionary<string, int> GetCacheStatistics()
        {
            var entries = GetAllCacheEntries();
            var stats = new Dictionary<string, int>();
            
            foreach (CachePrefix prefix in Enum.GetValues(typeof(CachePrefix)))
            {
                var prefixString = prefix.ToPrefix();
                var count = entries.Count(e => e.Key.StartsWith(prefixString));
                stats[prefix.ToString()] = count;
            }
            
            stats["Total"] = entries.Count;
            return stats;
        }

        /// <summary>
        /// Clear all cache entries by prefix
        /// </summary>
        public int ClearByPrefix(CachePrefix prefix)
        {
            var entries = SearchByPrefix(prefix);
            foreach (var entry in entries)
            {
                _cache.Remove(entry.Key);
            }
            return entries.Count;
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
                CachePrefix.RoomTypeInventory => TimeSpan.FromMinutes(15),
                _ => TimeSpan.FromHours(1)
            };
        }
    }

    /// <summary>
    /// Model đại diện cho một cache entry
    /// </summary>
    public class CacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public object? Value { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
        public TimeSpan? TimeToLive => ExpiresAt.HasValue ? ExpiresAt.Value - DateTimeOffset.UtcNow : null;
    }
}
