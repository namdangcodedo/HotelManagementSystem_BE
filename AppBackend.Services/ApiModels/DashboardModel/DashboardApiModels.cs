using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.DashboardModel
{
    #region Overview Statistics DTOs

    /// <summary>
    /// Dashboard overview statistics
    /// </summary>
    public class DashboardOverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCustomers { get; set; }
        public decimal OccupancyRate { get; set; }
        public decimal AverageBookingValue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int BookingsGrowth { get; set; }
        public int ActiveBookings { get; set; }
        public int PendingPayments { get; set; }
    }

    /// <summary>
    /// Revenue data point for charts
    /// </summary>
    public class RevenueDataPoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
        public decimal AverageValue { get; set; }
    }

    /// <summary>
    /// Booking statistics data point
    /// </summary>
    public class BookingDataPoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public int OnlineBookings { get; set; }
        public int WalkinBookings { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// Request for statistics with date range
    /// </summary>
    public class DashboardStatsRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? GroupBy { get; set; } = "day"; // day, week, month, year
    }

    #endregion

    #region Top Lists DTOs

    /// <summary>
    /// Top room item
    /// </summary>
    public class TopRoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal OccupancyRate { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Top customer item
    /// </summary>
    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public string CustomerTier { get; set; } = "Regular"; // Regular, VIP, Premium
    }

    /// <summary>
    /// Top room type item
    /// </summary>
    public class TopRoomTypeDto
    {
        public int RoomTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int AvailableRooms { get; set; }
        public decimal PopularityScore { get; set; }
    }

    /// <summary>
    /// Request for top lists
    /// </summary>
    public class TopListRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Limit { get; set; } = 10;
    }

    #endregion

    #region Recent Activities DTOs

    /// <summary>
    /// Recent booking item
    /// </summary>
    public class RecentBookingDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string RoomNumbers { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Recent payment item
    /// </summary>
    public class RecentPaymentDto
    {
        public int TransactionId { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// System alert item
    /// </summary>
    public class SystemAlertDto
    {
        public string AlertType { get; set; } = string.Empty; // CheckIn, CheckOut, LowInventory, PaymentDue
        public string Severity { get; set; } = "Info"; // Info, Warning, Critical
        public string Message { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedId { get; set; }
    }

    #endregion

    #region Detailed Reports DTOs

    /// <summary>
    /// Revenue report data
    /// </summary>
    public class RevenueReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal CardRevenue { get; set; }
        public decimal OnlineRevenue { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public List<RevenueDataPoint> DailyRevenue { get; set; } = new();
        public Dictionary<string, decimal> RevenueByRoomType { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    }

    /// <summary>
    /// Occupancy report data
    /// </summary>
    public class OccupancyReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal AverageOccupancyRate { get; set; }
        public int TotalRooms { get; set; }
        public int TotalBookedNights { get; set; }
        public int TotalAvailableNights { get; set; }
        public List<OccupancyDataPoint> DailyOccupancy { get; set; } = new();
        public Dictionary<string, decimal> OccupancyByRoomType { get; set; } = new();
    }

    /// <summary>
    /// Occupancy data point
    /// </summary>
    public class OccupancyDataPoint
    {
        public DateTime Date { get; set; }
        public int OccupiedRooms { get; set; }
        public int TotalRooms { get; set; }
        public decimal OccupancyRate { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Customer report data
    /// </summary>
    public class CustomerReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal AverageSpendPerCustomer { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public List<CustomerSegmentData> CustomerSegments { get; set; } = new();
        public List<TopCustomerDto> TopSpenders { get; set; } = new();
    }

    /// <summary>
    /// Customer segment data
    /// </summary>
    public class CustomerSegmentData
    {
        public string SegmentName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Report request
    /// </summary>
    public class ReportRequest
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public string Format { get; set; } = "json"; // json, pdf, excel
    }

    #endregion

    #region Real-time Data DTOs

    /// <summary>
    /// Live occupancy data
    /// </summary>
    public class LiveOccupancyDto
    {
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public decimal OccupancyPercentage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<RoomStatusBreakdown> RoomsByType { get; set; } = new();
    }

    /// <summary>
    /// Room status breakdown
    /// </summary>
    public class RoomStatusBreakdown
    {
        public string RoomTypeName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Occupied { get; set; }
        public int Available { get; set; }
        public decimal OccupancyRate { get; set; }
    }

    /// <summary>
    /// Today's bookings data
    /// </summary>
    public class TodayBookingsDto
    {
        public DateTime Date { get; set; }
        public int TotalBookings { get; set; }
        public int CheckIns { get; set; }
        public int CheckOuts { get; set; }
        public int NewBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public List<RecentBookingDto> UpcomingCheckIns { get; set; } = new();
        public List<RecentBookingDto> UpcomingCheckOuts { get; set; } = new();
    }

    /// <summary>
    /// Pending tasks data
    /// </summary>
    public class PendingTasksDto
    {
        public int PendingCheckIns { get; set; }
        public int PendingCheckOuts { get; set; }
        public int RoomsToClean { get; set; }
        public int PendingPayments { get; set; }
        public int MaintenanceRequests { get; set; }
        public int UnresolvedComplaints { get; set; }
        public List<TaskItem> UrgentTasks { get; set; } = new();
    }

    /// <summary>
    /// Task item
    /// </summary>
    public class TaskItem
    {
        public string TaskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
        public DateTime DueTime { get; set; }
        public int? RelatedId { get; set; }
        public string? ActionUrl { get; set; }
    }

    #endregion

    #region Summary Response DTOs

    /// <summary>
    /// Complete dashboard data response
    /// </summary>
    public class DashboardSummaryDto
    {
        public DashboardOverviewDto Overview { get; set; } = new();
        public List<RevenueDataPoint> RevenueChart { get; set; } = new();
        public List<TopRoomDto> TopRooms { get; set; } = new();
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
        public List<RecentBookingDto> RecentBookings { get; set; } = new();
        public List<SystemAlertDto> Alerts { get; set; } = new();
        public LiveOccupancyDto LiveOccupancy { get; set; } = new();
        public TodayBookingsDto TodayStats { get; set; } = new();
    }

    #endregion
}
