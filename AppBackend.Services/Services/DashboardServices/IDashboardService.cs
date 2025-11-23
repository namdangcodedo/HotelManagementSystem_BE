using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.DashboardModel;

namespace AppBackend.Services.Services.DashboardServices
{
    public interface IDashboardService
    {
        #region Overview Statistics

        /// <summary>
        /// Get dashboard overview statistics
        /// </summary>
        Task<ResultModel> GetDashboardOverviewAsync(DashboardStatsRequest request);

        /// <summary>
        /// Get revenue statistics with time grouping
        /// </summary>
        Task<ResultModel> GetRevenueStatisticsAsync(DashboardStatsRequest request);

        /// <summary>
        /// Get booking statistics with time grouping
        /// </summary>
        Task<ResultModel> GetBookingStatisticsAsync(DashboardStatsRequest request);

        #endregion

        #region Top Lists

        /// <summary>
        /// Get top rooms by booking count and revenue
        /// </summary>
        Task<ResultModel> GetTopRoomsAsync(TopListRequest request);

        /// <summary>
        /// Get top customers by spending
        /// </summary>
        Task<ResultModel> GetTopCustomersAsync(TopListRequest request);

        /// <summary>
        /// Get top room types
        /// </summary>
        Task<ResultModel> GetTopRoomTypesAsync(TopListRequest request);

        #endregion

        #region Recent Activities

        /// <summary>
        /// Get recent bookings
        /// </summary>
        Task<ResultModel> GetRecentBookingsAsync(int limit = 20);

        /// <summary>
        /// Get recent payments
        /// </summary>
        Task<ResultModel> GetRecentPaymentsAsync(int limit = 20);

        /// <summary>
        /// Get system alerts
        /// </summary>
        Task<ResultModel> GetSystemAlertsAsync();

        #endregion

        #region Detailed Reports

        /// <summary>
        /// Get revenue report
        /// </summary>
        Task<ResultModel> GetRevenueReportAsync(ReportRequest request);

        /// <summary>
        /// Get occupancy report
        /// </summary>
        Task<ResultModel> GetOccupancyReportAsync(ReportRequest request);

        /// <summary>
        /// Get customer report
        /// </summary>
        Task<ResultModel> GetCustomerReportAsync(ReportRequest request);

        #endregion

        #region Real-time Data

        /// <summary>
        /// Get live occupancy data
        /// </summary>
        Task<ResultModel> GetLiveOccupancyAsync();

        /// <summary>
        /// Get today's bookings data
        /// </summary>
        Task<ResultModel> GetTodayBookingsAsync();

        /// <summary>
        /// Get pending tasks
        /// </summary>
        Task<ResultModel> GetPendingTasksAsync();

        #endregion

        #region Dashboard Summary

        /// <summary>
        /// Get complete dashboard summary (all data in one call)
        /// </summary>
        Task<ResultModel> GetDashboardSummaryAsync(DashboardStatsRequest request);

        #endregion
    }
}
