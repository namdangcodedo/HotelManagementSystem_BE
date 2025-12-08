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

        #region Helper Methods

        /// <summary>
        /// Get week of year
        /// </summary>
        private static int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date,
                System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        /// <summary>
        /// Get first date of week
        /// </summary>
        private static DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            var firstMonday = jan1.AddDays(daysOffset);
            var firstWeek = GetWeekOfYear(jan1) == 1 ? firstMonday : firstMonday.AddDays(7);
            return firstWeek.AddDays((weekOfYear - 1) * 7);
        }

        #endregion

        #region Overview Statistics

        public async Task<ResultModel> GetDashboardOverviewAsync(DashboardStatsRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                // Get CommonCode IDs using CommonCodeHelper
                var paidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Paid");
                var unpaidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Unpaid");

                // Optimized queries - execute in parallel
                var totalBookingsTask = _context.Bookings
                    .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
                    .CountAsync();

                var totalCustomersTask = _context.Bookings
                    .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .CountAsync();

                // Use Task<decimal?> for SumAsync to represent nullable results
                var totalRevenueTask = paidStatusId.HasValue
                    ? _context.Transactions
                        .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate && t.PaymentStatusId == paidStatusId.Value)
                        .SumAsync(t => (decimal?)t.PaidAmount)
                    : Task.FromResult<decimal?>(0);

                var totalRoomsTask = _context.Rooms.CountAsync();

                var bookedRoomsTask = _context.BookingRooms
                    .Where(br => br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)
                    .Select(br => br.RoomId)
                    .Distinct()
                    .CountAsync();

                var activeBookingsTask = _context.Bookings
                    .Where(b => b.CheckInDate <= DateTime.UtcNow && b.CheckOutDate >= DateTime.UtcNow)
                    .CountAsync();

                var pendingPaymentsTask = unpaidStatusId.HasValue
                    ? _context.Transactions
                        .Where(t => t.PaymentStatusId == unpaidStatusId.Value)
                        .CountAsync()
                    : Task.FromResult(0);

                // Previous period for growth calculation
                var previousPeriodStart = fromDate.AddMonths(-1);
                var previousBookingsTask = _context.Bookings
                    .Where(b => b.CreatedAt >= previousPeriodStart && b.CreatedAt < fromDate)
                    .CountAsync();

                var previousRevenueTask = paidStatusId.HasValue
                    ? _context.Transactions
                        .Where(t => t.CreatedAt >= previousPeriodStart && t.CreatedAt < fromDate && t.PaymentStatusId == paidStatusId.Value)
                        .SumAsync(t => (decimal?)t.PaidAmount)
                    : Task.FromResult<decimal?>(0);

                // Wait for all queries
                await Task.WhenAll(totalBookingsTask, totalCustomersTask, totalRevenueTask,
                    totalRoomsTask, bookedRoomsTask, activeBookingsTask, pendingPaymentsTask,
                    previousBookingsTask, previousRevenueTask);

                var totalBookings = await totalBookingsTask;
                var totalCustomers = await totalCustomersTask;
                var totalRevenue = (await totalRevenueTask) ?? 0m;
                var totalRooms = await totalRoomsTask;
                var bookedRooms = await bookedRoomsTask;
                var activeBookings = await activeBookingsTask;
                var pendingPayments = await pendingPaymentsTask;
                var previousBookings = await previousBookingsTask;
                var previousRevenue = (await previousRevenueTask) ?? 0m;

                // Calculate metrics
                var occupancyRate = totalRooms > 0 ? (decimal)bookedRooms / totalRooms * 100 : 0;
                var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;
                var revenueGrowth = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;
                var bookingsGrowth = previousBookings > 0 ? ((decimal)(totalBookings - previousBookings) / previousBookings) * 100 : 0;

                var overview = new DashboardOverviewDto
                {
                    TotalRevenue = totalRevenue,
                    TotalBookings = totalBookings,
                    TotalCustomers = totalCustomers,
                    OccupancyRate = Math.Round(occupancyRate, 2),
                    AverageBookingValue = Math.Round(averageBookingValue, 2),
                    RevenueGrowth = Math.Round(revenueGrowth, 2),
                    BookingsGrowth = (int)Math.Round(bookingsGrowth),
                    ActiveBookings = activeBookings,
                    PendingPayments = pendingPayments
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = overview,
                    Message = "Dashboard overview retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving dashboard overview: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRevenueStatisticsAsync(DashboardStatsRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;
                var groupBy = request.GroupBy?.ToLower() ?? "day";

                var paidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Paid");
                if (!paidStatusId.HasValue)
                {
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = "SUCCESS",
                        StatusCode = 200,
                        Data = new List<RevenueDataPoint>(),
                        Message = "No paid status configured"
                    };
                }

                var transactions = await _context.Transactions
                    .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate && t.PaymentStatusId == paidStatusId.Value)
                    .Select(t => new { t.CreatedAt, t.PaidAmount })
                    .ToListAsync();

                var revenueData = new List<RevenueDataPoint>();

                if (groupBy == "day")
                {
                    revenueData = transactions
                        .GroupBy(t => t.CreatedAt.Date)
                        .Select(g => new RevenueDataPoint
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            Revenue = g.Sum(t => t.PaidAmount),
                            BookingCount = g.Count(),
                            AverageValue = g.Count() > 0 ? g.Sum(t => t.PaidAmount) / g.Count() : 0
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }
                else if (groupBy == "week")
                {
                    revenueData = transactions
                        .GroupBy(t => new { Year = t.CreatedAt.Year, Week = GetWeekOfYear(t.CreatedAt) })
                        .Select(g => new RevenueDataPoint
                        {
                            Date = FirstDateOfWeek(g.Key.Year, g.Key.Week),
                            Label = $"Week {g.Key.Week}",
                            Revenue = g.Sum(t => t.PaidAmount),
                            BookingCount = g.Count(),
                            AverageValue = g.Count() > 0 ? g.Sum(t => t.PaidAmount) / g.Count() : 0
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }
                else if (groupBy == "month")
                {
                    revenueData = transactions
                        .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                        .Select(g => new RevenueDataPoint
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            Revenue = g.Sum(t => t.PaidAmount),
                            BookingCount = g.Count(),
                            AverageValue = g.Count() > 0 ? g.Sum(t => t.PaidAmount) / g.Count() : 0
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }
                else if (groupBy == "year")
                {
                    revenueData = transactions
                        .GroupBy(t => t.CreatedAt.Year)
                        .Select(g => new RevenueDataPoint
                        {
                            Date = new DateTime(g.Key, 1, 1),
                            Label = g.Key.ToString(),
                            Revenue = g.Sum(t => t.PaidAmount),
                            BookingCount = g.Count(),
                            AverageValue = g.Count() > 0 ? g.Sum(t => t.PaidAmount) / g.Count() : 0
                        })
                        .OrderBy(r => r.Date)
                        .ToList();
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = revenueData,
                    Message = "Revenue statistics retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue statistics");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving revenue statistics: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetBookingStatisticsAsync(DashboardStatsRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow.AddMonths(-1);
                var groupBy = request.GroupBy?.ToLower() ?? "day";

                var onlineTypeId = await _commonCodeHelper.GetCommonCodeIdAsync("BookingType", "Online");
                var walkinTypeId = await _commonCodeHelper.GetCommonCodeIdAsync("BookingType", "WalkIn");

                var bookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
                    .Select(b => new { b.CreatedAt, b.BookingTypeId, b.TotalAmount })
                    .ToListAsync();

                var bookingData = new List<BookingDataPoint>();

                if (groupBy == "day")
                {
                    bookingData = bookings
                        .GroupBy(b => b.CreatedAt.Date)
                        .Select(g => new BookingDataPoint
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            OnlineBookings = onlineTypeId.HasValue ? g.Count(b => b.BookingTypeId == onlineTypeId.Value) : 0,
                            WalkinBookings = walkinTypeId.HasValue ? g.Count(b => b.BookingTypeId == walkinTypeId.Value) : 0,
                            TotalBookings = g.Count(),
                            TotalRevenue = g.Sum(b => b.TotalAmount)
                        })
                        .OrderBy(b => b.Date)
                        .ToList();
                }
                else if (groupBy == "month")
                {
                    bookingData = bookings
                        .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                        .Select(g => new BookingDataPoint
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Label = $"{g.Key.Month}/{g.Key.Year}",
                            OnlineBookings = onlineTypeId.HasValue ? g.Count(b => b.BookingTypeId == onlineTypeId.Value) : 0,
                            WalkinBookings = walkinTypeId.HasValue ? g.Count(b => b.BookingTypeId == walkinTypeId.Value) : 0,
                            TotalBookings = g.Count(),
                            TotalRevenue = g.Sum(b => b.TotalAmount)
                        })
                        .OrderBy(b => b.Date)
                        .ToList();
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = bookingData,
                    Message = "Booking statistics retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking statistics");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving booking statistics: {ex.Message}"
                };
            }
        }

        #endregion

        #region Top Lists

        public async Task<ResultModel> GetTopRoomsAsync(TopListRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                var topRooms = await _context.BookingRooms
                    .Where(br => br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)
                    .GroupBy(br => new
                    {
                        br.RoomId,
                        br.Room.RoomName,
                        RoomTypeName = br.Room.RoomType != null ? br.Room.RoomType.TypeName : ""
                    })
                    .Select(g => new TopRoomDto
                    {
                        RoomId = g.Key.RoomId,
                        RoomNumber = g.Key.RoomName,
                        RoomTypeName = g.Key.RoomTypeName,
                        BookingCount = g.Count(),
                        TotalRevenue = g.Sum(br => br.SubTotal),
                        OccupancyRate = 0, // Can calculate if needed
                        ImageUrl = null
                    })
                    .OrderByDescending(r => r.BookingCount)
                    .Take(request.Limit)
                    .ToListAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = topRooms,
                    Message = "Top rooms retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top rooms");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving top rooms: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetTopCustomersAsync(TopListRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                var topCustomers = await _context.Bookings
                    .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
                    .GroupBy(b => new
                    {
                        b.CustomerId,
                        b.Customer.FullName,
                        Email = b.Customer.Account != null ? b.Customer.Account.Email : "",
                        b.Customer.PhoneNumber
                    })
                    .Select(g => new
                    {
                        g.Key.CustomerId,
                        g.Key.FullName,
                        g.Key.Email,
                        g.Key.PhoneNumber,
                        BookingCount = g.Count(),
                        TotalSpent = g.Sum(b => b.TotalAmount),
                        LastBookingDate = g.Max(b => b.CreatedAt)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(request.Limit)
                    .ToListAsync();

                var result = topCustomers.Select(c => new TopCustomerDto
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName ?? "",
                    Email = c.Email ?? "",
                    PhoneNumber = c.PhoneNumber,
                    BookingCount = c.BookingCount,
                    TotalSpent = c.TotalSpent,
                    LastBookingDate = c.LastBookingDate,
                    CustomerTier = c.TotalSpent > 50000000 ? "Premium" : (c.TotalSpent > 20000000 ? "VIP" : "Regular")
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = result,
                    Message = "Top customers retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving top customers: {ex.Message}"
                };
            }
        }

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
                    .OrderByDescending(rt => rt.BookingCount)
                    .Take(request.Limit)
                    .ToListAsync();

                var result = topRoomTypes.Select(rt => new TopRoomTypeDto
                {
                    RoomTypeId = rt.RoomTypeId,
                    TypeName = rt.TypeName,
                    BookingCount = rt.BookingCount,
                    TotalRevenue = rt.TotalRevenue,
                    AveragePrice = rt.BookingCount > 0 ? rt.TotalRevenue / rt.BookingCount : 0,
                    AvailableRooms = 0, // Can fetch if needed
                    PopularityScore = rt.BookingCount * 10 + (rt.TotalRevenue / 1000000)
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = result,
                    Message = "Top room types retrieved successfully"
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

        #region Recent Activities

        public async Task<ResultModel> GetRecentBookingsAsync(int limit = 20)
        {
            try
            {
                var commonCodes = await _commonCodeHelper.GetCachedCommonCodesAsync();

                var bookings = await _context.Bookings
                    .Include(b => b.Customer)
                        .ThenInclude(c => c.Account)
                    .Include(b => b.BookingRooms)
                        .ThenInclude(br => br.Room)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(limit)
                    .Select(b => new
                    {
                        b.BookingId,
                        CustomerName = b.Customer != null ? b.Customer.FullName : "",
                        RoomNumbers = string.Join(", ", b.BookingRooms.Select(br => br.Room.RoomName)),
                        b.CheckInDate,
                        b.CheckOutDate,
                        b.TotalAmount,
                        b.StatusId,
                        b.BookingTypeId,
                        b.CreatedAt
                    })
                    .ToListAsync();

                var result = bookings.Select(b => new RecentBookingDto
                {
                    BookingId = b.BookingId,
                    BookingReference = $"BK{b.BookingId}",
                    CustomerName = b.CustomerName,
                    RoomNumbers = b.RoomNumbers,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    TotalAmount = b.TotalAmount,
                    BookingStatus = commonCodes.FirstOrDefault(c => c.CodeId == b.StatusId).CodeValue ?? "",
                    BookingType = commonCodes.FirstOrDefault(c => c.CodeId == b.BookingTypeId).CodeValue ?? "",
                    CreatedAt = b.CreatedAt
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = result,
                    Message = "Recent bookings retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent bookings");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving recent bookings: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetRecentPaymentsAsync(int limit = 20)
        {
            try
            {
                var commonCodes = await _commonCodeHelper.GetCachedCommonCodesAsync();

                var payments = await _context.Transactions
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Customer)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(limit)
                    .Select(t => new
                    {
                        t.TransactionId,
                        t.TransactionRef,
                        t.BookingId,
                        CustomerName = t.Booking != null && t.Booking.Customer != null ? t.Booking.Customer.FullName : "",
                        t.PaidAmount,
                        t.PaymentMethodId,
                        t.PaymentStatusId,
                        t.CreatedAt
                    })
                    .ToListAsync();

                var result = payments.Select(t => new RecentPaymentDto
                {
                    TransactionId = t.TransactionId,
                    TransactionRef = t.TransactionRef ?? $"TXN{t.TransactionId}",
                    BookingId = t.BookingId,
                    CustomerName = t.CustomerName,
                    Amount = t.PaidAmount,
                    PaymentMethod = commonCodes.FirstOrDefault(c => c.CodeId == t.PaymentMethodId).CodeValue ?? "",
                    PaymentStatus = commonCodes.FirstOrDefault(c => c.CodeId == t.PaymentStatusId).CodeValue ?? "",
                    CreatedAt = t.CreatedAt
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = result,
                    Message = "Recent payments retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent payments");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving recent payments: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetSystemAlertsAsync()
        {
            try
            {
                var alerts = new List<SystemAlertDto>();
                var today = DateTime.UtcNow.Date;

                // Check-ins today
                var checkInsCount = await _context.Bookings
                    .Where(b => b.CheckInDate.Date == today)
                    .CountAsync();

                if (checkInsCount > 0)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        AlertType = "CheckIn",
                        Severity = "Info",
                        Message = $"{checkInsCount} check-ins scheduled for today",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Check-outs today
                var checkOutsCount = await _context.Bookings
                    .Where(b => b.CheckOutDate.Date == today)
                    .CountAsync();

                if (checkOutsCount > 0)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        AlertType = "CheckOut",
                        Severity = "Info",
                        Message = $"{checkOutsCount} check-outs scheduled for today",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Pending payments
                var unpaidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Unpaid");
                if (unpaidStatusId.HasValue)
                {
                    var pendingPayments = await _context.Transactions
                        .Where(t => t.PaymentStatusId == unpaidStatusId.Value)
                        .CountAsync();

                    if (pendingPayments > 0)
                    {
                        alerts.Add(new SystemAlertDto
                        {
                            AlertType = "PaymentDue",
                            Severity = "Warning",
                            Message = $"{pendingPayments} payments pending",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = alerts,
                    Message = "System alerts retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system alerts");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving system alerts: {ex.Message}"
                };
            }
        }

        #endregion

        #region Detailed Reports

        public async Task<ResultModel> GetRevenueReportAsync(ReportRequest request)
        {
            try
            {
                var paidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Paid");
                var refundedStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Refunded");

                if (!paidStatusId.HasValue)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = "CONFIG_ERROR",
                        StatusCode = 400,
                        Message = "Payment status configuration not found"
                    };
                }

                var transactions = await _context.Transactions
                    .Where(t => t.CreatedAt >= request.FromDate && t.CreatedAt <= request.ToDate)
                    .Select(t => new { t.PaymentStatusId, t.PaymentMethodId, t.PaidAmount, t.TotalAmount, t.CreatedAt })
                    .ToListAsync();

                var paidTransactions = transactions.Where(t => t.PaymentStatusId == paidStatusId.Value).ToList();
                var totalRevenue = paidTransactions.Sum(t => t.PaidAmount);

                // Revenue by payment method
                var commonCodes = await _commonCodeHelper.GetCachedCommonCodesAsync();
                var revenueByMethod = paidTransactions
                    .GroupBy(t => t.PaymentMethodId)
                    .ToDictionary(
                        g => commonCodes.FirstOrDefault(c => c.CodeId == g.Key).CodeValue ?? "Unknown",
                        g => g.Sum(t => t.PaidAmount)
                    );

                var cashRevenue = revenueByMethod.GetValueOrDefault("Cash", 0);
                var cardRevenue = revenueByMethod.GetValueOrDefault("Card", 0);
                var onlineRevenue = revenueByMethod.GetValueOrDefault("PayOS", 0) + revenueByMethod.GetValueOrDefault("QR", 0);

                // Refunded amount
                var refundedAmount = refundedStatusId.HasValue
                    ? transactions.Where(t => t.PaymentStatusId == refundedStatusId.Value).Sum(t => t.TotalAmount - t.PaidAmount)
                    : 0;

                // Daily revenue
                var dailyRevenue = paidTransactions
                    .GroupBy(t => t.CreatedAt.Date)
                    .Select(g => new RevenueDataPoint
                    {
                        Date = g.Key,
                        Label = g.Key.ToString("dd/MM/yyyy"),
                        Revenue = g.Sum(t => t.PaidAmount),
                        BookingCount = g.Count(),
                        AverageValue = g.Count() > 0 ? g.Sum(t => t.PaidAmount) / g.Count() : 0
                    })
                    .OrderBy(r => r.Date)
                    .ToList();

                var report = new RevenueReportDto
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    TotalRevenue = totalRevenue,
                    CashRevenue = cashRevenue,
                    CardRevenue = cardRevenue,
                    OnlineRevenue = onlineRevenue,
                    RefundedAmount = refundedAmount,
                    NetRevenue = totalRevenue - refundedAmount,
                    DailyRevenue = dailyRevenue,
                    RevenueByPaymentMethod = revenueByMethod
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = report,
                    Message = "Revenue report retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue report");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving revenue report: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetOccupancyReportAsync(ReportRequest request)
        {
            try
            {
                var totalRooms = await _context.Rooms.CountAsync();

                var bookings = await _context.Bookings
                    .Where(b => (b.CheckInDate >= request.FromDate && b.CheckInDate <= request.ToDate) ||
                                (b.CheckOutDate >= request.FromDate && b.CheckOutDate <= request.ToDate) ||
                                (b.CheckInDate <= request.FromDate && b.CheckOutDate >= request.ToDate))
                    .Select(b => new { b.CheckInDate, b.CheckOutDate })
                    .ToListAsync();

                // Calculate daily occupancy
                var dailyOccupancy = new List<OccupancyDataPoint>();
                for (var date = request.FromDate.Date; date <= request.ToDate.Date; date = date.AddDays(1))
                {
                    var occupiedRooms = bookings.Count(b => b.CheckInDate <= date && b.CheckOutDate > date);
                    var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

                    dailyOccupancy.Add(new OccupancyDataPoint
                    {
                        Date = date,
                        OccupiedRooms = occupiedRooms,
                        TotalRooms = totalRooms,
                        OccupancyRate = Math.Round(occupancyRate, 2),
                        Revenue = 0
                    });
                }

                var averageOccupancy = dailyOccupancy.Count > 0 ? dailyOccupancy.Average(d => d.OccupancyRate) : 0;
                var totalBookedNights = dailyOccupancy.Sum(d => d.OccupiedRooms);
                var totalAvailableNights = totalRooms * dailyOccupancy.Count;

                var report = new OccupancyReportDto
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    AverageOccupancyRate = Math.Round(averageOccupancy, 2),
                    TotalRooms = totalRooms,
                    TotalBookedNights = totalBookedNights,
                    TotalAvailableNights = totalAvailableNights,
                    DailyOccupancy = dailyOccupancy
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = report,
                    Message = "Occupancy report retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting occupancy report");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving occupancy report: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetCustomerReportAsync(ReportRequest request)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Where(b => b.CreatedAt >= request.FromDate && b.CreatedAt <= request.ToDate)
                    .Select(b => new { b.CustomerId, b.TotalAmount, b.CreatedAt })
                    .ToListAsync();

                var totalCustomers = bookings.Select(b => b.CustomerId).Distinct().Count();

                // New vs returning customers
                var previousBookingCustomers = await _context.Bookings
                    .Where(b => b.CreatedAt < request.FromDate)
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .ToListAsync();

                var newCustomers = bookings
                    .Where(b => !previousBookingCustomers.Contains(b.CustomerId))
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .Count();

                var returningCustomers = totalCustomers - newCustomers;

                var totalSpent = bookings.Sum(b => b.TotalAmount);
                var averageSpend = totalCustomers > 0 ? totalSpent / totalCustomers : 0;

                var retentionRate = previousBookingCustomers.Count > 0
                    ? (decimal)returningCustomers / previousBookingCustomers.Count * 100
                    : 0;

                var report = new CustomerReportDto
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    TotalCustomers = totalCustomers,
                    NewCustomers = newCustomers,
                    ReturningCustomers = returningCustomers,
                    AverageSpendPerCustomer = Math.Round(averageSpend, 2),
                    CustomerRetentionRate = Math.Round(retentionRate, 2),
                    CustomerSegments = new List<CustomerSegmentData>(),
                    TopSpenders = new List<TopCustomerDto>()
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = report,
                    Message = "Customer report retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer report");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving customer report: {ex.Message}"
                };
            }
        }

        #endregion

        #region Real-time Data

        public async Task<ResultModel> GetLiveOccupancyAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var totalRooms = await _context.Rooms.CountAsync();

                // Rooms occupied today
                var occupiedRoomIds = await _context.Bookings
                    .Where(b => b.CheckInDate <= today && b.CheckOutDate > today)
                    .SelectMany(b => b.BookingRooms.Select(br => br.RoomId))
                    .Distinct()
                    .ToListAsync();

                var occupiedRooms = occupiedRoomIds.Count;

                // Maintenance rooms
                var maintenanceStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("RoomStatus", "Maintenance");
                var maintenanceRooms = maintenanceStatusId.HasValue
                    ? await _context.Rooms.CountAsync(r => r.StatusId == maintenanceStatusId.Value)
                    : 0;

                var availableRooms = totalRooms - occupiedRooms - maintenanceRooms;
                var occupancyPercentage = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

                var liveOccupancy = new LiveOccupancyDto
                {
                    TotalRooms = totalRooms,
                    OccupiedRooms = occupiedRooms,
                    AvailableRooms = Math.Max(0, availableRooms),
                    MaintenanceRooms = maintenanceRooms,
                    OccupancyPercentage = Math.Round(occupancyPercentage, 2),
                    UpdatedAt = DateTime.UtcNow,
                    RoomsByType = new List<RoomStatusBreakdown>()
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = liveOccupancy,
                    Message = "Live occupancy retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live occupancy");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving live occupancy: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetTodayBookingsAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var todayBookingsCount = await _context.Bookings
                    .Where(b => b.CreatedAt.Date == today)
                    .CountAsync();

                var checkInsCount = await _context.Bookings
                    .Where(b => b.CheckInDate.Date == today)
                    .CountAsync();

                var checkOutsCount = await _context.Bookings
                    .Where(b => b.CheckOutDate.Date == today)
                    .CountAsync();

                var todayBookings = await _context.Bookings
                    .Where(b => b.CreatedAt.Date == today)
                    .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

                var paidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("BookingStatus", "Completed");
                var paidAmount = paidStatusId.HasValue
                    ? await _context.Bookings
                        .Where(b => b.CreatedAt.Date == today && b.BookingId == paidStatusId.Value)
                        .SumAsync(b => (decimal?)b.TotalAmount) ?? 0
                    : 0;

                var pendingAmount = todayBookings - paidAmount;

                var todayData = new TodayBookingsDto
                {
                    Date = today,
                    TotalBookings = todayBookingsCount,
                    CheckIns = checkInsCount,
                    CheckOuts = checkOutsCount,
                    NewBookings = todayBookingsCount,
                    TotalRevenue = todayBookings,
                    PaidAmount = paidAmount,
                    PendingAmount = pendingAmount,
                    UpcomingCheckIns = new List<RecentBookingDto>(),
                    UpcomingCheckOuts = new List<RecentBookingDto>()
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = todayData,
                    Message = "Today's bookings retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's bookings");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving today's bookings: {ex.Message}"
                };
            }
        }

        public async Task<ResultModel> GetPendingTasksAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var pendingCheckIns = await _context.Bookings
                    .Where(b => b.CheckInDate.Date == today)
                    .CountAsync();

                var pendingCheckOuts = await _context.Bookings
                    .Where(b => b.CheckOutDate.Date == today)
                    .CountAsync();

                var unpaidStatusId = await _commonCodeHelper.GetCommonCodeIdAsync("PaymentStatus", "Unpaid");
                var pendingPayments = unpaidStatusId.HasValue
                    ? await _context.Transactions.CountAsync(t => t.PaymentStatusId == unpaidStatusId.Value)
                    : 0;

                var tasks = new PendingTasksDto
                {
                    PendingCheckIns = pendingCheckIns,
                    PendingCheckOuts = pendingCheckOuts,
                    RoomsToClean = 0,
                    PendingPayments = pendingPayments,
                    MaintenanceRequests = 0,
                    UnresolvedComplaints = 0,
                    UrgentTasks = new List<TaskItem>()
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = tasks,
                    Message = "Pending tasks retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending tasks");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving pending tasks: {ex.Message}"
                };
            }
        }

        #endregion

        #region Dashboard Summary

        public async Task<ResultModel> GetDashboardSummaryAsync(DashboardStatsRequest request)
        {
            try
            {
                // Execute all tasks in parallel for best performance
                var overviewTask = GetDashboardOverviewAsync(request);
                var revenueTask = GetRevenueStatisticsAsync(request);
                var topRoomsTask = GetTopRoomsAsync(new TopListRequest { FromDate = request.FromDate, ToDate = request.ToDate, Limit = 5 });
                var topCustomersTask = GetTopCustomersAsync(new TopListRequest { FromDate = request.FromDate, ToDate = request.ToDate, Limit = 5 });
                var recentBookingsTask = GetRecentBookingsAsync(10);
                var alertsTask = GetSystemAlertsAsync();
                var liveOccupancyTask = GetLiveOccupancyAsync();
                var todayBookingsTask = GetTodayBookingsAsync();

                await Task.WhenAll(overviewTask, revenueTask, topRoomsTask, topCustomersTask,
                    recentBookingsTask, alertsTask, liveOccupancyTask, todayBookingsTask);

                var summary = new DashboardSummaryDto
                {
                    Overview = (await overviewTask).Data as DashboardOverviewDto ?? new(),
                    RevenueChart = (await revenueTask).Data as List<RevenueDataPoint> ?? new(),
                    TopRooms = (await topRoomsTask).Data as List<TopRoomDto> ?? new(),
                    TopCustomers = (await topCustomersTask).Data as List<TopCustomerDto> ?? new(),
                    RecentBookings = (await recentBookingsTask).Data as List<RecentBookingDto> ?? new(),
                    Alerts = (await alertsTask).Data as List<SystemAlertDto> ?? new(),
                    LiveOccupancy = (await liveOccupancyTask).Data as LiveOccupancyDto ?? new(),
                    TodayStats = (await todayBookingsTask).Data as TodayBookingsDto ?? new()
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = summary,
                    Message = "Dashboard summary retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "SERVER_ERROR",
                    StatusCode = 500,
                    Message = $"Error retrieving dashboard summary: {ex.Message}"
                };
            }
        }

        #endregion
    }
}
