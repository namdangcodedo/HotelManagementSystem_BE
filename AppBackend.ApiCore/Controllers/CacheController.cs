using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Helpers;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing and searching memory cache (Admin only)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CacheController : BaseApiController
    {
        private readonly CacheHelper _cacheHelper;

        public CacheController(CacheHelper cacheHelper)
        {
            _cacheHelper = cacheHelper;
        }

        /// <summary>
        /// Search cache entries theo prefix
        /// </summary>
        /// <param name="prefix">Cache prefix để filter (RefreshToken, OtpCode, RoomBookingLock, v.v.)</param>
        /// <returns>Danh sách cache entries</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <remarks>
        /// Cache Prefixes có sẵn:
        /// - RefreshToken: Refresh tokens của users
        /// - AccessToken: Access tokens
        /// - UserSession: User sessions
        /// - OtpCode: OTP codes cho reset password
        /// - RoomBookingLock: Locks của phòng đang được booking
        /// - BookingPayment: Payment information của bookings
        /// - RoomTypeInventory: Inventory của room types
        /// - AccountActivation: Activation tokens
        /// 
        /// Nếu không truyền prefix, sẽ trả về tất cả entries
        /// </remarks>
        [HttpGet("search-by-prefix")]
        public IActionResult SearchByPrefix([FromQuery] CachePrefix? prefix = null)
        {
            var entries = _cacheHelper.SearchByPrefix(prefix);
            
            // Không trả về value thực tế để bảo mật, chỉ trả về metadata
            var result = entries.Select(e => new
            {
                e.Key,
                e.Type,
                e.ExpiresAt,
                e.IsExpired,
                TimeToLive = e.TimeToLive?.ToString(@"hh\:mm\:ss"),
                HasValue = e.Value != null
            }).ToList();

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = $"Tìm thấy {result.Count} cache entries",
                Data = new
                {
                    Prefix = prefix?.ToString() ?? "All",
                    Count = result.Count,
                    Entries = result
                }
            });
        }

        /// <summary>
        /// Search cache entries theo pattern (contains)
        /// </summary>
        /// <param name="pattern">Pattern để search trong cache key</param>
        /// <returns>Danh sách cache entries</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="400">Pattern không hợp lệ</response>
        [HttpGet("search-by-pattern")]
        public IActionResult SearchByPattern([FromQuery] string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return ValidationError("Pattern không được để trống");

            var entries = _cacheHelper.SearchByPattern(pattern);
            
            var result = entries.Select(e => new
            {
                e.Key,
                e.Type,
                e.ExpiresAt,
                e.IsExpired,
                TimeToLive = e.TimeToLive?.ToString(@"hh\:mm\:ss"),
                HasValue = e.Value != null
            }).ToList();

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = $"Tìm thấy {result.Count} cache entries với pattern '{pattern}'",
                Data = new
                {
                    Pattern = pattern,
                    Count = result.Count,
                    Entries = result
                }
            });
        }

        /// <summary>
        /// Get tất cả cache entries
        /// </summary>
        /// <returns>Danh sách tất cả cache entries</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("all")]
        public IActionResult GetAllEntries()
        {
            var entries = _cacheHelper.GetAllCacheEntries();
            
            var result = entries.Select(e => new
            {
                e.Key,
                e.Type,
                e.ExpiresAt,
                e.IsExpired,
                TimeToLive = e.TimeToLive?.ToString(@"hh\:mm\:ss"),
                HasValue = e.Value != null
            }).ToList();

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = $"Tổng cộng {result.Count} cache entries",
                Data = new
                {
                    Count = result.Count,
                    Entries = result
                }
            });
        }

        /// <summary>
        /// Get thống kê cache theo prefix
        /// </summary>
        /// <returns>Statistics về số lượng entries theo từng prefix</returns>
        /// <response code="200">Lấy thống kê thành công</response>
        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            var stats = _cacheHelper.GetCacheStatistics();

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = "Lấy thống kê cache thành công",
                Data = stats
            });
        }

        /// <summary>
        /// Get chi tiết một cache entry theo key
        /// </summary>
        /// <param name="key">Full cache key</param>
        /// <returns>Chi tiết cache entry</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy cache entry</response>
        [HttpGet("entry/{key}")]
        public IActionResult GetEntryByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return ValidationError("Key không được để trống");

            var entry = _cacheHelper.GetAllCacheEntries()
                .FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return NotFound(new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Không tìm thấy cache entry với key: {key}"
                });

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = "Lấy thông tin cache entry thành công",
                Data = new
                {
                    entry.Key,
                    entry.Type,
                    entry.ExpiresAt,
                    entry.IsExpired,
                    TimeToLive = entry.TimeToLive?.ToString(@"hh\:mm\:ss"),
                    Value = entry.Value // Admin có thể xem value
                }
            });
        }

        /// <summary>
        /// Clear tất cả cache entries theo prefix
        /// </summary>
        /// <param name="prefix">Cache prefix để clear</param>
        /// <returns>Số lượng entries đã xóa</returns>
        /// <response code="200">Clear thành công</response>
        /// <remarks>
        /// ⚠️ WARNING: Thao tác này sẽ xóa TẤT CẢ cache entries với prefix được chọn!
        /// 
        /// Ví dụ:
        /// - Clear RefreshToken: Sẽ logout tất cả users
        /// - Clear RoomBookingLock: Sẽ release tất cả room locks
        /// - Clear OtpCode: Sẽ xóa tất cả OTP codes đang còn hiệu lực
        /// </remarks>
        [HttpDelete("clear-by-prefix")]
        public IActionResult ClearByPrefix([FromQuery] CachePrefix prefix)
        {
            var count = _cacheHelper.ClearByPrefix(prefix);

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = $"Đã xóa {count} cache entries với prefix '{prefix}'",
                Data = new
                {
                    Prefix = prefix.ToString(),
                    DeletedCount = count
                }
            });
        }

        /// <summary>
        /// Remove một cache entry theo key
        /// </summary>
        /// <param name="key">Full cache key để xóa</param>
        /// <returns>Kết quả xóa</returns>
        /// <response code="200">Xóa thành công</response>
        [HttpDelete("entry/{key}")]
        public IActionResult RemoveEntry(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return ValidationError("Key không được để trống");

            _cacheHelper.RemoveCustom(key);

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = $"Đã xóa cache entry: {key}",
                Data = new { Key = key }
            });
        }

        /// <summary>
        /// Get danh sách các cache prefixes có sẵn
        /// </summary>
        /// <returns>Danh sách prefixes</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("prefixes")]
        public IActionResult GetAvailablePrefixes()
        {
            var prefixes = Enum.GetValues(typeof(CachePrefix))
                .Cast<CachePrefix>()
                .Select(p => new
                {
                    Name = p.ToString(),
                    Prefix = p.ToPrefix(),
                    Description = GetPrefixDescription(p)
                })
                .ToList();

            return Ok(new ResultModel
            {
                IsSuccess = true,
                Message = "Danh sách cache prefixes",
                Data = prefixes
            });
        }

        private string GetPrefixDescription(CachePrefix prefix)
        {
            return prefix switch
            {
                CachePrefix.RefreshToken => "Refresh tokens của users (TTL: 7 days)",
                CachePrefix.AccessToken => "Access tokens (TTL: 30 minutes)",
                CachePrefix.UserSession => "User sessions (TTL: 2 hours)",
                CachePrefix.OtpCode => "OTP codes cho reset password",
                CachePrefix.RoomBookingLock => "Locks của phòng đang được booking (TTL: 10 minutes)",
                CachePrefix.BookingPayment => "Payment information của bookings (TTL: 15 minutes)",
                CachePrefix.RoomTypeInventory => "Inventory của room types (TTL: 15 minutes)",
                CachePrefix.AccountActivation => "Account activation tokens",
                _ => "Cache entries"
            };
        }
    }
}

