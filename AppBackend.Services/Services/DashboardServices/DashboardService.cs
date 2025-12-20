using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.DashboardModel;
using AppBackend.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.DashboardServices
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HotelManagementContext _context;
        private readonly CommonCodeHelper _commonCodeHelper;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IUnitOfWork unitOfWork,
            HotelManagementContext context,
            CommonCodeHelper commonCodeHelper,
            ILogger<DashboardService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _commonCodeHelper = commonCodeHelper;
            _logger = logger;
        }

        #region Main Dashboard API

        /// <summary>
        /// Get complete dashboard statistics (Priority: HIGH)
        /// Returns all statistics needed for the dashboard in one API call
        /// </summary>
        public async Task<ResultModel> GetDashboardStatsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var thisMonthStart = new DateTime(now.Year, now.Month, 1);
                var lastMonthStart = thisMonthStart.AddMonths(-1);
                var lastMonthEnd = thisMonthStart.AddDays(-1);

                // Get CommonCode IDs - FIX: Use CodeName (English) instead of CodeValue (Vietnamese)
                var completedTransactionStatusId = await _commonCodeHelper.GetCommonCodeIdByNameAsync("TransactionStatus", "Completed");
                var pendingTransactionStatusId = await _commonCodeHelper.GetCommonCodeIdByNameAsync("TransactionStatus", "Pending");
                var maintenanceStatusId = await _commonCodeHelper.GetCommonCodeIdByNameAsync("RoomStatus", "Maintenance");

                // DEBUG: Log the CommonCode IDs
                _logger.LogInformation("Dashboard Stats - Completed TransactionStatusId: {CompletedId}, Pending: {PendingId}", 
                    completedTransactionStatusId, pendingTransactionStatusId);

                // === BOOKING STATISTICS ===
                var totalBookings = await _context.Bookings.CountAsync();
                var bookingsThisMonth = await _context.Bookings
                    .Where(b => b.CreatedAt >= thisMonthStart)
                    .CountAsync();
                var bookingsLastMonth = await _context.Bookings
                    .Where(b => b.CreatedAt >= lastMonthStart && b.CreatedAt <= lastMonthEnd)
                    .CountAsync();

                // === REVENUE STATISTICS ===
                // DEBUG: Log all transactions to see what's in the database
                var allTransactions = await _context.Transactions
                    .Include(t => t.TransactionStatus)
                    .Include(t => t.PaymentStatus)
                    .ToListAsync();
                
                _logger.LogInformation("Dashboard Stats - Total Transactions in DB: {Count}", allTransactions.Count);
                foreach (var trans in allTransactions.Take(5))
                {
                    _logger.LogInformation("Transaction {Id}: TransactionStatusId={TransStatusId}, TransactionStatus={TransStatus}, PaymentStatusId={PayStatusId}, PaymentStatus={PayStatus}, PaidAmount={PaidAmount}",
                        trans.TransactionId, 
                        trans.TransactionStatusId,
                        trans.TransactionStatus?.CodeName ?? "NULL",
                        trans.PaymentStatusId,
                        trans.PaymentStatus?.CodeName ?? "NULL",
                        trans.PaidAmount);
                }

                // FIX: Calculate revenue from completed transactions (TransactionStatus = "Completed")
                var totalRevenue = completedTransactionStatusId.HasValue
                    ? (await _context.Transactions
                        .Where(t => t.TransactionStatusId == completedTransactionStatusId.Value)
                        .SumAsync(t => (decimal?)t.PaidAmount)) ?? 0m
                    : 0m;

                var revenueThisMonth = completedTransactionStatusId.HasValue
                    ? (await _context.Transactions
                        .Where(t => t.TransactionStatusId == completedTransactionStatusId.Value && t.CreatedAt >= thisMonthStart)
                        .SumAsync(t => (decimal?)t.PaidAmount)) ?? 0m
                    : 0m;

                var revenueLastMonth = completedTransactionStatusId.HasValue
                    ? (await _context.Transactions
                        .Where(t => t.TransactionStatusId == completedTransactionStatusId.Value &&
                                    t.CreatedAt >= lastMonthStart &&
                                    t.CreatedAt <= lastMonthEnd)
                        .SumAsync(t => (decimal?)t.PaidAmount)) ?? 0m
                    : 0m;

                _logger.LogInformation("Dashboard Stats - Total Revenue: {TotalRevenue}, This Month: {ThisMonth}, Last Month: {LastMonth}",
                    totalRevenue, revenueThisMonth, revenueLastMonth);

                // Calculate average room rate from completed bookings
                var completedBookings = await _context.Bookings
                    .Where(b => b.TotalAmount > 0)
                    .Select(b => new { b.TotalAmount, Nights = EF.Functions.DateDiffDay(b.CheckInDate, b.CheckOutDate) })
                    .ToListAsync();

                var averageRoomRate = completedBookings.Any()
                    ? completedBookings
                        .Where(b => b.Nights > 0)
                        .Average(b => b.TotalAmount / b.Nights)
                    : 0m;

                // === CUSTOMER STATISTICS ===
                var totalCustomers = await _context.Customers.CountAsync();

                var newCustomersThisMonth = await _context.Customers
                    .Where(c => c.CreatedAt >= thisMonthStart)
                    .CountAsync();

                var newCustomersLastMonth = await _context.Customers
                    .Where(c => c.CreatedAt >= lastMonthStart && c.CreatedAt <= lastMonthEnd)
                    .CountAsync();

                // === ROOM STATISTICS ===
                var totalRooms = await _context.Rooms.CountAsync();

                var maintenanceRooms = maintenanceStatusId.HasValue
                    ? await _context.Rooms.CountAsync(r => r.StatusId == maintenanceStatusId.Value)
                    : 0;

                // Get currently occupied rooms (active bookings)
                var occupiedRooms = await _context.Bookings
                    .Where(b => b.CheckInDate <= now && b.CheckOutDate > now)
                    .SelectMany(b => b.BookingRooms.Select(br => br.RoomId))
                    .Distinct()
                    .CountAsync();

                var availableRooms = totalRooms - occupiedRooms - maintenanceRooms;

                // === TRANSACTION STATISTICS ===
                // FIX: Use TransactionStatus to count completed and pending payments
                var totalTransactions = await _context.Transactions.CountAsync();

                var completedPayments = completedTransactionStatusId.HasValue
                    ? await _context.Transactions.CountAsync(t => t.TransactionStatusId == completedTransactionStatusId.Value)
                    : 0;

                var pendingPayments = pendingTransactionStatusId.HasValue
                    ? await _context.Transactions.CountAsync(t => t.TransactionStatusId == pendingTransactionStatusId.Value)
                    : 0;

                _logger.LogInformation("Dashboard Stats - Completed Payments: {Completed}, Pending Payments: {Pending}",
                    completedPayments, pendingPayments);

                // Calculate growth rates
                var bookingsGrowth = bookingsLastMonth > 0
                    ? ((decimal)(bookingsThisMonth - bookingsLastMonth) / bookingsLastMonth) * 100
                    : 0m;

                var revenueGrowth = revenueLastMonth > 0
                    ? ((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100
                    : 0m;

                var customersGrowth = newCustomersLastMonth > 0
                    ? ((decimal)(newCustomersThisMonth - newCustomersLastMonth) / newCustomersLastMonth) * 100
                    : 0m;

                var occupancyRate = totalRooms > 0
                    ? ((decimal)occupiedRooms / totalRooms) * 100
                    : 0m;

                var stats = new DashboardStatsDto
                {
                    // Booking stats
                    TotalBookings = totalBookings,
                    BookingsThisMonth = bookingsThisMonth,
                    BookingsLastMonth = bookingsLastMonth,
                    BookingsGrowth = Math.Round(bookingsGrowth, 1),

                    // Revenue stats
                    TotalRevenue = Math.Round(totalRevenue, 0),
                    RevenueThisMonth = Math.Round(revenueThisMonth, 0),
                    RevenueLastMonth = Math.Round(revenueLastMonth, 0),
                    RevenueGrowth = Math.Round(revenueGrowth, 1),
                    AverageRoomRate = Math.Round(averageRoomRate, 0),

                    // Customer stats
                    TotalCustomers = totalCustomers,
                    NewCustomersThisMonth = newCustomersThisMonth,
                    CustomersGrowth = Math.Round(customersGrowth, 1),

                    // Room stats
                    TotalRooms = totalRooms,
                    AvailableRooms = Math.Max(0, availableRooms),
                    OccupiedRooms = occupiedRooms,
                    MaintenanceRooms = maintenanceRooms,
                    OccupancyRate = Math.Round(occupancyRate, 1),

                    // Transaction stats
                    TotalTransactions = totalTransactions,
                    CompletedPayments = completedPayments,
                    PendingPayments = pendingPayments
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = stats,
                    Message = "Get statistics successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving dashboard statistics: {ex.Message}"
                };
            }
        }

        #endregion

        #region Optional Dashboard APIs

        /// <summary>
        /// Get room status breakdown (Priority: MEDIUM - Optional)
        /// Can be calculated from stats API, but provided as separate endpoint for convenience
        /// </summary>
        public async Task<ResultModel> GetRoomStatusAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Get CommonCode IDs
                var maintenanceStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("RoomStatus", "Maintenance");

                var totalRooms = await _context.Rooms.CountAsync();

                var maintenanceRooms = maintenanceStatusId.HasValue
                    ? await _context.Rooms.CountAsync(r => r.StatusId == maintenanceStatusId.Value)
                    : 0;

                // Get currently occupied rooms (active bookings)
                var occupiedRooms = await _context.Bookings
                    .Where(b => b.CheckInDate <= now && b.CheckOutDate > now)
                    .SelectMany(b => b.BookingRooms.Select(br => br.RoomId))
                    .Distinct()
                    .CountAsync();

                var availableRooms = totalRooms - occupiedRooms - maintenanceRooms;

                var roomStatus = new List<RoomStatusDto>
                {
                    new RoomStatusDto
                    {
                        Status = "available",
                        Count = Math.Max(0, availableRooms),
                        Percentage = totalRooms > 0 ? Math.Round(((decimal)availableRooms / totalRooms) * 100, 1) : 0
                    },
                    new RoomStatusDto
                    {
                        Status = "occupied",
                        Count = occupiedRooms,
                        Percentage = totalRooms > 0 ? Math.Round(((decimal)occupiedRooms / totalRooms) * 100, 1) : 0
                    },
                    new RoomStatusDto
                    {
                        Status = "maintenance",
                        Count = maintenanceRooms,
                        Percentage = totalRooms > 0 ? Math.Round(((decimal)maintenanceRooms / totalRooms) * 100, 1) : 0
                    }
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = roomStatus,
                    Message = "Get room status successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting room status");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving room status: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get revenue by month (Priority: LOW - For future chart feature)
        /// </summary>
        public async Task<ResultModel> GetRevenueByMonthAsync(RevenueByMonthRequest request)
        {
            try
            {
                var paidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Paid");
                if (!paidStatusId.HasValue)
                {
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 200,
                        Data = new List<RevenueByMonthDto>(),
                        Message = "No paid status configured"
                    };
                }

                var monthsToFetch = Math.Max(1, Math.Min(request.Months, 24)); // Limit to 1-24 months
                var startDate = DateTime.UtcNow.AddMonths(-monthsToFetch).Date;

                var transactions = await _context.Transactions
                    .Where(t => t.PaymentStatusId == paidStatusId.Value && t.CreatedAt >= startDate)
                    .Select(t => new { t.CreatedAt, t.PaidAmount })
                    .ToListAsync();

                var bookingCounts = await _context.Bookings
                    .Where(b => b.CreatedAt >= startDate)
                    .Select(b => b.CreatedAt)
                    .ToListAsync();

                var revenueByMonth = transactions
                    .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                    .Select(g => new RevenueByMonthDto
                    {
                        Month = g.Key.Month.ToString("00"),
                        Year = g.Key.Year,
                        Revenue = Math.Round(g.Sum(t => t.PaidAmount), 0),
                        Bookings = bookingCounts.Count(b => b.Year == g.Key.Year && b.Month == g.Key.Month)
                    })
                    .OrderBy(r => r.Year)
                    .ThenBy(r => r.Month)
                    .ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = revenueByMonth,
                    Message = "Get revenue by month successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue by month");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving revenue by month: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get top room types (Priority: LOW - For future analytics feature)
        /// </summary>
        public async Task<ResultModel> GetTopRoomTypesAsync(TopListRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                var topRoomTypes = await _context.BookingRooms
                    .Where(br => br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)
                    .GroupBy(br => new
                    {
                        RoomTypeId = br.Room.RoomTypeId,
                        TypeName = br.Room.RoomType != null ? br.Room.RoomType.TypeName : ""
                    })
                    .Select(g => new
                    {
                        g.Key.RoomTypeId,
                        g.Key.TypeName,
                        BookingCount = g.Count(),
                        TotalRevenue = g.Sum(br => br.SubTotal)
                    })
                    .OrderByDescending(rt => rt.TotalRevenue)
                    .Take(request.Limit)
                    .ToListAsync();

                var result = topRoomTypes.Select(rt => new TopRoomTypeDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    BookingCount = rt.BookingCount,
                    TotalRevenue = Math.Round(rt.TotalRevenue, 0),
                    AveragePrice = rt.BookingCount > 0 ? Math.Round(rt.TotalRevenue / rt.BookingCount, 0) : 0
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = result,
                    Message = "Get top room types successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top room types");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving top room types: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
