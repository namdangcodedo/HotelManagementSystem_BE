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

        /// <summary>
        /// Get complete dashboard statistics (all stats in one call)
        /// Priority: HIGH - Required for dashboard
        /// Returns: Booking stats, revenue stats, customer stats, room stats, transaction stats
        /// </summary>
        /// <remarks>
        /// This is the main API endpoint for the dashboard.
        /// Frontend will call this endpoint every 60 seconds to refresh data.
        ///
        /// Response includes:
        /// - Booking statistics (total, this month, last month, growth)
        /// - Revenue statistics (total, this month, last month, growth, average room rate)
        /// - Customer statistics (total, new this month, growth)
        /// - Room statistics (total, available, occupied, maintenance, occupancy rate)
        /// - Transaction statistics (total, completed payments, pending payments)
        /// </remarks>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(DashboardStatsDto), 200)]
        public async Task<IActionResult> GetDashboardStats()
        {
            var result = await _dashboardService.GetDashboardStatsAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get room status breakdown (available, occupied, maintenance)
        /// Priority: MEDIUM - Optional, can be calculated from stats API
        /// </summary>
        /// <remarks>
        /// This endpoint provides detailed room status breakdown.
        /// Note: This data can also be derived from the /stats endpoint.
        /// Frontend will call this every 30 seconds if needed.
        /// </remarks>
        [HttpGet("room-status")]
        [ProducesResponseType(typeof(List<RoomStatusDto>), 200)]
        public async Task<IActionResult> GetRoomStatus()
        {
            var result = await _dashboardService.GetRoomStatusAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get revenue statistics by month
        /// Priority: LOW - For future chart feature
        /// </summary>
        /// <param name="months">Number of months to retrieve (default: 12, max: 24)</param>
        /// <remarks>
        /// This endpoint is prepared for future chart/analytics features.
        /// Not currently used by the frontend dashboard.
        /// </remarks>
        [HttpGet("revenue-by-month")]
        [ProducesResponseType(typeof(List<RevenueByMonthDto>), 200)]
        public async Task<IActionResult> GetRevenueByMonth([FromQuery] int months = 12)
        {
            var request = new RevenueByMonthRequest { Months = months };
            var result = await _dashboardService.GetRevenueByMonthAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Get top room types by bookings and revenue
        /// Priority: LOW - For future analytics feature
        /// </summary>
        /// <param name="limit">Number of top room types to return (default: 5)</param>
        /// <remarks>
        /// This endpoint is prepared for future analytics features.
        /// Not currently used by the frontend dashboard.
        /// </remarks>
        [HttpGet("top-room-types")]
        [ProducesResponseType(typeof(List<TopRoomTypeDto>), 200)]
        public async Task<IActionResult> GetTopRoomTypes([FromQuery] int limit = 5)
        {
            var request = new TopListRequest { Limit = limit };
            var result = await _dashboardService.GetTopRoomTypesAsync(request);
            return HandleResult(result);
        }
    }
}
