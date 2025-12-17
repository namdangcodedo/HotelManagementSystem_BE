using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.DashboardModel;

namespace AppBackend.Services.Services.DashboardServices
{
    public interface IDashboardService
    {
        /// <summary>
        /// Get complete dashboard statistics (all stats in one call)
        /// Priority: HIGH - Required for dashboard
        /// Endpoint: GET /Dashboard/stats
        /// </summary>
        Task<ResultModel> GetDashboardStatsAsync();

        /// <summary>
        /// Get room status breakdown (available, occupied, maintenance)
        /// Priority: MEDIUM - Optional, can be calculated from stats
        /// Endpoint: GET /Dashboard/room-status
        /// </summary>
        Task<ResultModel> GetRoomStatusAsync();

        /// <summary>
        /// Get revenue statistics by month
        /// Priority: LOW - For future chart feature
        /// Endpoint: GET /Dashboard/revenue-by-month
        /// </summary>
        Task<ResultModel> GetRevenueByMonthAsync(RevenueByMonthRequest request);

        /// <summary>
        /// Get top room types by bookings and revenue
        /// Priority: LOW - For future analytics feature
        /// Endpoint: GET /Dashboard/top-room-types
        /// </summary>
        Task<ResultModel> GetTopRoomTypesAsync(TopListRequest request);
    }
}
