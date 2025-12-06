using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Helpers
{
    /// <summary>
    /// Centralized helper for CommonCode operations with caching support
    /// </summary>
    public class CommonCodeHelper
    {
        private readonly HotelManagementContext _context;
        private readonly CacheHelper _cacheHelper;
        private readonly ILogger<CommonCodeHelper> _logger;

        // Cache keys
        private const string CACHE_KEY_COMMON_CODES = "all_common_codes";
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public CommonCodeHelper(
            HotelManagementContext context,
            CacheHelper cacheHelper,
            ILogger<CommonCodeHelper> logger)
        {
            _context = context;
            _cacheHelper = cacheHelper;
            _logger = logger;
        }

        #region Get CommonCode by Type and Value

        /// <summary>
        /// Get CommonCode ID by type and value with caching
        /// </summary>
        public async Task<int?> GetCommonCodeIdAsync(string codeType, string codeValue)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                var code = commonCodes.FirstOrDefault(c => 
                    c.CodeType == codeType && 
                    c.CodeValue.Equals(codeValue, StringComparison.OrdinalIgnoreCase));
                
                return code?.CodeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCode: {codeType} - {codeValue}");
                return null;
            }
        }

        /// <summary>
        /// Get CommonCode ID by type and name with caching
        /// </summary>
        public async Task<int?> GetCommonCodeIdByNameAsync(string codeType, string codeName)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                var code = commonCodes.FirstOrDefault(c => 
                    c.CodeType == codeType && 
                    c.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
                
                return code?.CodeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCode by name: {codeType} - {codeName}");
                return null;
            }
        }

        /// <summary>
        /// Get CommonCode entity by type and value with caching
        /// </summary>
        public async Task<CommonCode?> GetCommonCodeAsync(string codeType, string codeValue)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                return commonCodes.FirstOrDefault(c => 
                    c.CodeType == codeType && 
                    c.CodeValue.Equals(codeValue, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCode: {codeType} - {codeValue}");
                return null;
            }
        }

        /// <summary>
        /// Get CommonCode entity by type and name with caching
        /// </summary>
        public async Task<CommonCode?> GetCommonCodeByNameAsync(string codeType, string codeName)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                return commonCodes.FirstOrDefault(c => 
                    c.CodeType == codeType && 
                    c.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCode by name: {codeType} - {codeName}");
                return null;
            }
        }

        #endregion

        #region Get CommonCode by ID

        /// <summary>
        /// Get CommonCode by ID with caching
        /// </summary>
        public async Task<CommonCode?> GetCommonCodeByIdAsync(int codeId)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                return commonCodes.FirstOrDefault(c => c.CodeId == codeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCode by ID: {codeId}");
                return null;
            }
        }

        #endregion

        #region Get Multiple CommonCodes

        /// <summary>
        /// Get all CommonCodes by type with caching
        /// </summary>
        public async Task<List<CommonCode>> GetCommonCodesByTypeAsync(string codeType)
        {
            try
            {
                var commonCodes = await GetCachedCommonCodesInternalAsync();
                return commonCodes.Where(c => c.CodeType == codeType).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting CommonCodes by type: {codeType}");
                return new List<CommonCode>();
            }
        }

        /// <summary>
        /// Get all active CommonCodes with caching
        /// </summary>
        public async Task<List<CommonCode>> GetAllCommonCodesAsync()
        {
            try
            {
                return await GetCachedCommonCodesInternalAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all CommonCodes");
                return new List<CommonCode>();
            }
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Get all CommonCodes with caching (internal method)
        /// </summary>
        private async Task<List<CommonCode>> GetCachedCommonCodesInternalAsync()
        {
            var cached = _cacheHelper.Get<List<CommonCode>>(CachePrefix.UserSession, CACHE_KEY_COMMON_CODES);
            if (cached != null)
            {
                return cached;
            }

            var commonCodes = await _context.CommonCodes
                .Where(c => c.IsActive)
                .AsNoTracking()
                .ToListAsync();

            _cacheHelper.Set(CachePrefix.UserSession, CACHE_KEY_COMMON_CODES, commonCodes, _cacheExpiration);
            return commonCodes;
        }

        /// <summary>
        /// Get all CommonCodes as tuples (for backward compatibility with DashboardService)
        /// </summary>
        public async Task<List<(int CodeId, string CodeType, string CodeValue, string CodeName)>> GetCachedCommonCodesAsync()
        {
            var commonCodes = await GetCachedCommonCodesInternalAsync();
            return commonCodes.Select(c => (c.CodeId, c.CodeType, c.CodeValue, c.CodeName)).ToList();
        }

        /// <summary>
        /// Clear CommonCode cache (use when CommonCode data is updated)
        /// </summary>
        public void ClearCache()
        {
            _cacheHelper.Remove(CachePrefix.UserSession, CACHE_KEY_COMMON_CODES);
            _logger.LogInformation("CommonCode cache cleared");
        }

        /// <summary>
        /// Refresh CommonCode cache
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            ClearCache();
            await GetCachedCommonCodesInternalAsync();
            _logger.LogInformation("CommonCode cache refreshed");
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Check if a CommonCode exists
        /// </summary>
        public async Task<bool> ExistsAsync(string codeType, string codeValue)
        {
            var code = await GetCommonCodeAsync(codeType, codeValue);
            return code != null;
        }

        /// <summary>
        /// Check if a CommonCode ID is valid
        /// </summary>
        public async Task<bool> IsValidIdAsync(int codeId, string? codeType = null)
        {
            var code = await GetCommonCodeByIdAsync(codeId);
            if (code == null) return false;
            
            if (!string.IsNullOrEmpty(codeType))
            {
                return code.CodeType == codeType;
            }
            
            return true;
        }

        #endregion
    }
}
