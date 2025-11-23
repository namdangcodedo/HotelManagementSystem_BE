using AppBackend.Services.ApiModels.DashboardModel;
using AppBackend.Services.Services.DashboardServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Manager,Admin")]
    public class DashboardController : BaseApiController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        #region Overview Statistics

        /// <summary>
        /// Get dashboard overview statistics
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetDashboardOverview([FromQuery] DashboardStatsRequest request)
        {
            var result = await _dashboardService.GetDashboardOverviewAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get revenue statistics with time grouping
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueStatistics([FromQuery] DashboardStatsRequest request)
        {
            var result = await _dashboardService.GetRevenueStatisticsAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get booking statistics with time grouping
        /// </summary>
        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookingStatistics([FromQuery] DashboardStatsRequest request)
        {
            var result = await _dashboardService.GetBookingStatisticsAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Top Lists

        /// <summary>
        /// Get top rooms by booking count and revenue
        /// </summary>
        [HttpGet("top-rooms")]
        public async Task<IActionResult> GetTopRooms([FromQuery] TopListRequest request)
        {
            var result = await _dashboardService.GetTopRoomsAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get top customers by spending
        /// </summary>
        [HttpGet("top-customers")]
        public async Task<IActionResult> GetTopCustomers([FromQuery] TopListRequest request)
        {
            var result = await _dashboardService.GetTopCustomersAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get top room types
        /// </summary>
        [HttpGet("top-room-types")]
        public async Task<IActionResult> GetTopRoomTypes([FromQuery] TopListRequest request)
        {
            var result = await _dashboardService.GetTopRoomTypesAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Recent Activities

        /// <summary>
        /// Get recent bookings
        /// </summary>
        [HttpGet("recent-bookings")]
        public async Task<IActionResult> GetRecentBookings([FromQuery] int limit = 20)
        {
            var result = await _dashboardService.GetRecentBookingsAsync(limit);
            return HandleResult(result);
        }

        /// <summary>
        /// Get recent payments
        /// </summary>
        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments([FromQuery] int limit = 20)
        {
            var result = await _dashboardService.GetRecentPaymentsAsync(limit);
            return HandleResult(result);
        }

        /// <summary>
        /// Get system alerts
        /// </summary>
        [HttpGet("alerts")]
        public async Task<IActionResult> GetSystemAlerts()
        {
            var result = await _dashboardService.GetSystemAlertsAsync();
            return HandleResult(result);
        }

        #endregion

        #region Detailed Reports

        /// <summary>
        /// Get revenue report
        /// </summary>
        [HttpGet("reports/revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] ReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _dashboardService.GetRevenueReportAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get occupancy report
        /// </summary>
        [HttpGet("reports/occupancy")]
        public async Task<IActionResult> GetOccupancyReport([FromQuery] ReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _dashboardService.GetOccupancyReportAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get customer report
        /// </summary>
        [HttpGet("reports/customers")]
        public async Task<IActionResult> GetCustomerReport([FromQuery] ReportRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _dashboardService.GetCustomerReportAsync(request);
            return HandleResult(result);
        }

        #endregion

        #region Real-time Data

        /// <summary>
        /// Get live occupancy data
        /// </summary>
        [HttpGet("live/occupancy")]
        public async Task<IActionResult> GetLiveOccupancy()
        {
            var result = await _dashboardService.GetLiveOccupancyAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get today's bookings data
        /// </summary>
        [HttpGet("live/today-bookings")]
        public async Task<IActionResult> GetTodayBookings()
        {
            var result = await _dashboardService.GetTodayBookingsAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get pending tasks
        /// </summary>
        [HttpGet("live/pending-tasks")]
        public async Task<IActionResult> GetPendingTasks()
        {
            var result = await _dashboardService.GetPendingTasksAsync();
            return HandleResult(result);
        }

        #endregion

        #region Dashboard Summary

        /// <summary>
        /// Get complete dashboard summary (all data in one call)
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] DashboardStatsRequest request)
        {
            var result = await _dashboardService.GetDashboardSummaryAsync(request);
            return HandleResult(result);
        }

        #endregion
    }
}
