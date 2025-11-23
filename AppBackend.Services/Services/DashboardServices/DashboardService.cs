using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.DashboardModel;
using AppBackend.Services.Helpers;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.DashboardServices
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IUnitOfWork unitOfWork,
            CacheHelper cacheHelper,
            ILogger<DashboardService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _logger = logger;
        }

        #region Overview Statistics

        public async Task<ResultModel> GetDashboardOverviewAsync(DashboardStatsRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = request.ToDate ?? DateTime.UtcNow;

                // Get all bookings in date range
                var bookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate)).ToList();

                // Get all transactions
                var transactions = (await _unitOfWork.Transactions.FindAsync(t =>
                    t.CreatedAt >= fromDate && t.CreatedAt <= toDate)).ToList();

                // Get payment status codes
                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid")).FirstOrDefault();

                // Calculate total revenue
                var totalRevenue = transactions
                    .Where(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId)
                    .Sum(t => t.PaidAmount);

                // Total bookings
                var totalBookings = bookings.Count;

                // Unique customers
                var totalCustomers = bookings.Select(b => b.CustomerId).Distinct().Count();

                // Calculate occupancy rate
                var totalRooms = (await _unitOfWork.Rooms.GetAllAsync()).Count();
                var bookedRooms = bookings.SelectMany(b => b.BookingRooms).Select(br => br.RoomId).Distinct().Count();
                var occupancyRate = totalRooms > 0 ? (decimal)bookedRooms / totalRooms * 100 : 0;

                // Average booking value
                var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

                // Get previous period for growth calculation
                var previousPeriodStart = fromDate.AddMonths(-1);
                var previousBookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt >= previousPeriodStart && b.CreatedAt < fromDate)).Count();

                var previousTransactions = (await _unitOfWork.Transactions.FindAsync(t =>
                    t.CreatedAt >= previousPeriodStart && t.CreatedAt < fromDate)).ToList();

                var previousRevenue = previousTransactions
                    .Where(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId)
                    .Sum(t => t.PaidAmount);

                // Calculate growth
                var revenueGrowth = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;
                var bookingsGrowth = previousBookings > 0 ? ((totalBookings - previousBookings) / previousBookings) * 100 : 0;

                // Active bookings and pending payments
                var activeBookings = bookings.Count(b => b.CheckInDate <= DateTime.UtcNow && b.CheckOutDate >= DateTime.UtcNow);

                var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid")).FirstOrDefault();
                var pendingPayments = transactions.Count(t => unpaidStatus != null && t.PaymentStatusId == unpaidStatus.CodeId);

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

                var transactions = (await _unitOfWork.Transactions.FindAsync(t =>
                    t.CreatedAt >= fromDate && t.CreatedAt <= toDate)).ToList();

                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid")).FirstOrDefault();

                var paidTransactions = transactions.Where(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId).ToList();

                var revenueData = new List<RevenueDataPoint>();

                if (groupBy == "day")
                {
                    revenueData = paidTransactions
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
                    revenueData = paidTransactions
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
                    revenueData = paidTransactions
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
                    revenueData = paidTransactions
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
                var toDate = request.ToDate ?? DateTime.UtcNow;
                var groupBy = request.GroupBy?.ToLower() ?? "day";

                var bookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate)).ToList();

                // Get booking type codes
                var onlineType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeValue == "Online")).FirstOrDefault();
                var walkinType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeValue == "WalkIn")).FirstOrDefault();

                var bookingData = new List<BookingDataPoint>();

                if (groupBy == "day")
                {
                    bookingData = bookings
                        .GroupBy(b => b.CreatedAt.Date)
                        .Select(g => new BookingDataPoint
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            OnlineBookings = g.Count(b => onlineType != null && b.BookingTypeId == onlineType.CodeId),
                            WalkinBookings = g.Count(b => walkinType != null && b.BookingTypeId == walkinType.CodeId),
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
                            OnlineBookings = g.Count(b => onlineType != null && b.BookingTypeId == onlineType.CodeId),
                            WalkinBookings = g.Count(b => walkinType != null && b.BookingTypeId == walkinType.CodeId),
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

                var bookingRooms = (await _unitOfWork.BookingRooms.FindAsync(br =>
                    br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)).ToList();

                var rooms = await _unitOfWork.Rooms.GetAllAsync();
                var roomsList = rooms.ToList();

                var topRooms = bookingRooms
                    .GroupBy(br => br.RoomId)
                    .Select(g =>
                    {
                        var room = roomsList.FirstOrDefault(r => r.RoomId == g.Key);
                        return new TopRoomDto
                        {
                            RoomId = g.Key,
                            RoomNumber = room?.RoomNumber ?? "",
                            RoomTypeName = room?.RoomType?.TypeName ?? "",
                            BookingCount = g.Count(),
                            TotalRevenue = g.Sum(br => br.RoomPrice * (decimal)(br.Booking.CheckOutDate - br.Booking.CheckInDate).TotalDays),
                            OccupancyRate = 0, // Calculate if needed
                            ImageUrl = room?.Media?.FirstOrDefault()?.Url
                        };
                    })
                    .OrderByDescending(r => r.BookingCount)
                    .Take(request.Limit)
                    .ToList();

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

                var bookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt >= fromDate && b.CreatedAt <= toDate)).ToList();

                var customers = await _unitOfWork.Customers.GetAllAsync();
                var customersList = customers.ToList();

                var topCustomers = bookings
                    .GroupBy(b => b.CustomerId)
                    .Select(g =>
                    {
                        var customer = customersList.FirstOrDefault(c => c.CustomerId == g.Key);
                        var totalSpent = g.Sum(b => b.TotalAmount);
                        var bookingCount = g.Count();

                        return new TopCustomerDto
                        {
                            CustomerId = g.Key,
                            FullName = customer?.FullName ?? "",
                            Email = customer?.Email ?? "",
                            PhoneNumber = customer?.PhoneNumber,
                            BookingCount = bookingCount,
                            TotalSpent = totalSpent,
                            LastBookingDate = g.Max(b => b.CreatedAt),
                            CustomerTier = totalSpent > 50000000 ? "Premium" : (totalSpent > 20000000 ? "VIP" : "Regular")
                        };
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(request.Limit)
                    .ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = topCustomers,
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

                var bookingRooms = (await _unitOfWork.BookingRooms.FindAsync(br =>
                    br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)).ToList();

                var roomTypes = await _unitOfWork.RoomTypes.GetAllAsync();
                var roomTypesList = roomTypes.ToList();

                var topRoomTypes = bookingRooms
                    .GroupBy(br => br.Room.RoomTypeId)
                    .Select(g =>
                    {
                        var roomType = roomTypesList.FirstOrDefault(rt => rt.RoomTypeId == g.Key);
                        var revenue = g.Sum(br => br.RoomPrice * (decimal)(br.Booking.CheckOutDate - br.Booking.CheckInDate).TotalDays);
                        var bookingCount = g.Count();

                        return new TopRoomTypeDto
                        {
                            RoomTypeId = g.Key,
                            TypeName = roomType?.TypeName ?? "",
                            BookingCount = bookingCount,
                            TotalRevenue = revenue,
                            AveragePrice = bookingCount > 0 ? revenue / bookingCount : 0,
                            AvailableRooms = roomType?.Rooms?.Count ?? 0,
                            PopularityScore = bookingCount * 10 + (revenue / 1000000)
                        };
                    })
                    .OrderByDescending(rt => rt.PopularityScore)
                    .Take(request.Limit)
                    .ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = topRoomTypes,
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
                var bookings = (await _unitOfWork.Bookings.GetAllAsync())
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(limit)
                    .ToList();

                var customers = await _unitOfWork.Customers.GetAllAsync();
                var customersList = customers.ToList();

                var paymentStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentStatus")).ToList();
                var bookingTypes = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "BookingType")).ToList();

                var recentBookings = bookings.Select(b =>
                {
                    var customer = customersList.FirstOrDefault(c => c.CustomerId == b.CustomerId);
                    var paymentStatus = paymentStatuses.FirstOrDefault(ps => ps.CodeId == b.PaymentStatusId);
                    var bookingType = bookingTypes.FirstOrDefault(bt => bt.CodeId == b.BookingTypeId);

                    return new RecentBookingDto
                    {
                        BookingId = b.BookingId,
                        BookingReference = b.BookingReference ?? $"BK{b.BookingId}",
                        CustomerName = customer?.FullName ?? "",
                        RoomNumbers = string.Join(", ", b.BookingRooms.Select(br => br.Room.RoomNumber)),
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        TotalAmount = b.TotalAmount,
                        PaymentStatus = paymentStatus?.CodeValue ?? "",
                        BookingType = bookingType?.CodeValue ?? "",
                        CreatedAt = b.CreatedAt
                    };
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = recentBookings,
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
                var transactions = (await _unitOfWork.Transactions.GetAllAsync())
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(limit)
                    .ToList();

                var bookings = await _unitOfWork.Bookings.GetAllAsync();
                var bookingsList = bookings.ToList();

                var customers = await _unitOfWork.Customers.GetAllAsync();
                var customersList = customers.ToList();

                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentMethod")).ToList();
                var paymentStatuses = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentStatus")).ToList();

                var recentPayments = transactions.Select(t =>
                {
                    var booking = bookingsList.FirstOrDefault(b => b.BookingId == t.BookingId);
                    var customer = customersList.FirstOrDefault(c => c.CustomerId == booking?.CustomerId);
                    var paymentMethod = paymentMethods.FirstOrDefault(pm => pm.CodeId == t.PaymentMethodId);
                    var paymentStatus = paymentStatuses.FirstOrDefault(ps => ps.CodeId == t.PaymentStatusId);

                    return new RecentPaymentDto
                    {
                        TransactionId = t.TransactionId,
                        TransactionRef = t.TransactionRef ?? $"TXN{t.TransactionId}",
                        BookingId = t.BookingId,
                        CustomerName = customer?.FullName ?? "",
                        Amount = t.PaidAmount,
                        PaymentMethod = paymentMethod?.CodeValue ?? "",
                        PaymentStatus = paymentStatus?.CodeValue ?? "",
                        CreatedAt = t.CreatedAt
                    };
                }).ToList();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = "SUCCESS",
                    StatusCode = 200,
                    Data = recentPayments,
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
                var tomorrow = today.AddDays(1);

                // Check-ins today
                var checkInsToday = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckInDate.Date == today)).ToList();

                if (checkInsToday.Any())
                {
                    alerts.Add(new SystemAlertDto
                    {
                        AlertType = "CheckIn",
                        Severity = "Info",
                        Message = $"{checkInsToday.Count} check-ins scheduled for today",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Check-outs today
                var checkOutsToday = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckOutDate.Date == today)).ToList();

                if (checkOutsToday.Any())
                {
                    alerts.Add(new SystemAlertDto
                    {
                        AlertType = "CheckOut",
                        Severity = "Info",
                        Message = $"{checkOutsToday.Count} check-outs scheduled for today",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Pending payments
                var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid")).FirstOrDefault();

                if (unpaidStatus != null)
                {
                    var pendingPayments = (await _unitOfWork.Transactions.FindAsync(t =>
                        t.PaymentStatusId == unpaidStatus.CodeId)).Count();

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
                var transactions = (await _unitOfWork.Transactions.FindAsync(t =>
                    t.CreatedAt >= request.FromDate && t.CreatedAt <= request.ToDate)).ToList();

                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid")).FirstOrDefault();

                var paidTransactions = transactions.Where(t => paidStatus != null && t.PaymentStatusId == paidStatus.CodeId).ToList();

                var totalRevenue = paidTransactions.Sum(t => t.PaidAmount);

                // Revenue by payment method
                var paymentMethods = (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "PaymentMethod")).ToList();
                var revenueByMethod = paidTransactions
                    .GroupBy(t => t.PaymentMethodId)
                    .ToDictionary(
                        g => paymentMethods.FirstOrDefault(pm => pm.CodeId == g.Key)?.CodeValue ?? "Unknown",
                        g => g.Sum(t => t.PaidAmount)
                    );

                var cashRevenue = revenueByMethod.GetValueOrDefault("Cash", 0);
                var cardRevenue = revenueByMethod.GetValueOrDefault("Card", 0);
                var onlineRevenue = revenueByMethod.GetValueOrDefault("PayOS", 0) + revenueByMethod.GetValueOrDefault("QR", 0);

                // Refunded amount
                var refundedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Refunded")).FirstOrDefault();
                var refundedAmount = transactions
                    .Where(t => refundedStatus != null && t.PaymentStatusId == refundedStatus.CodeId)
                    .Sum(t => t.TotalAmount - t.PaidAmount);

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
                var totalRooms = (await _unitOfWork.Rooms.GetAllAsync()).Count();

                var bookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    (b.CheckInDate >= request.FromDate && b.CheckInDate <= request.ToDate) ||
                    (b.CheckOutDate >= request.FromDate && b.CheckOutDate <= request.ToDate) ||
                    (b.CheckInDate <= request.FromDate && b.CheckOutDate >= request.ToDate))).ToList();

                // Calculate daily occupancy
                var dailyOccupancy = new List<OccupancyDataPoint>();
                for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
                {
                    var occupiedRooms = bookings.Count(b => b.CheckInDate <= date && b.CheckOutDate > date);
                    var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

                    dailyOccupancy.Add(new OccupancyDataPoint
                    {
                        Date = date,
                        OccupiedRooms = occupiedRooms,
                        TotalRooms = totalRooms,
                        OccupancyRate = Math.Round(occupancyRate, 2),
                        Revenue = 0 // Can calculate from bookings if needed
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
                var bookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt >= request.FromDate && b.CreatedAt <= request.ToDate)).ToList();

                var allCustomers = await _unitOfWork.Customers.GetAllAsync();
                var totalCustomers = bookings.Select(b => b.CustomerId).Distinct().Count();

                // New vs returning customers
                var previousBookings = await _unitOfWork.Bookings.FindAsync(b => b.CreatedAt < request.FromDate);
                var previousCustomerIds = previousBookings.Select(b => b.CustomerId).Distinct().ToList();

                var newCustomers = bookings.Where(b => !previousCustomerIds.Contains(b.CustomerId))
                    .Select(b => b.CustomerId)
                    .Distinct()
                    .Count();

                var returningCustomers = totalCustomers - newCustomers;

                var totalSpent = bookings.Sum(b => b.TotalAmount);
                var averageSpend = totalCustomers > 0 ? totalSpent / totalCustomers : 0;

                var retentionRate = previousCustomerIds.Count > 0
                    ? (decimal)returningCustomers / previousCustomerIds.Count * 100
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

                var allRooms = (await _unitOfWork.Rooms.GetAllAsync()).ToList();
                var totalRooms = allRooms.Count;

                // Rooms occupied today
                var activeBookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckInDate <= today && b.CheckOutDate > today)).ToList();

                var occupiedRoomIds = activeBookings.SelectMany(b => b.BookingRooms.Select(br => br.RoomId)).Distinct().ToList();
                var occupiedRooms = occupiedRoomIds.Count;

                // Maintenance rooms (assuming there's a status for this)
                var maintenanceStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "RoomStatus" && c.CodeValue == "Maintenance")).FirstOrDefault();
                var maintenanceRooms = maintenanceStatus != null
                    ? allRooms.Count(r => r.StatusId == maintenanceStatus.CodeId)
                    : 0;

                var availableRooms = totalRooms - occupiedRooms - maintenanceRooms;
                var occupancyPercentage = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

                var liveOccupancy = new LiveOccupancyDto
                {
                    TotalRooms = totalRooms,
                    OccupiedRooms = occupiedRooms,
                    AvailableRooms = availableRooms,
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

                var todayBookings = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CreatedAt.Date == today)).ToList();

                var checkIns = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckInDate.Date == today)).ToList();

                var checkOuts = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckOutDate.Date == today)).ToList();

                var totalRevenue = todayBookings.Sum(b => b.TotalAmount);

                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Paid")).FirstOrDefault();

                var paidAmount = todayBookings
                    .Where(b => paidStatus != null && b.PaymentStatusId == paidStatus.CodeId)
                    .Sum(b => b.TotalAmount);

                var pendingAmount = totalRevenue - paidAmount;

                var todayData = new TodayBookingsDto
                {
                    Date = today,
                    TotalBookings = todayBookings.Count,
                    CheckIns = checkIns.Count,
                    CheckOuts = checkOuts.Count,
                    NewBookings = todayBookings.Count,
                    TotalRevenue = totalRevenue,
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

                var pendingCheckIns = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckInDate.Date == today)).Count();

                var pendingCheckOuts = (await _unitOfWork.Bookings.FindAsync(b =>
                    b.CheckOutDate.Date == today)).Count();

                var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeValue == "Unpaid")).FirstOrDefault();

                var pendingPayments = unpaidStatus != null
                    ? (await _unitOfWork.Transactions.FindAsync(t => t.PaymentStatusId == unpaidStatus.CodeId)).Count()
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
                // Get all data in parallel for better performance
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
                    Overview = overviewTask.Result.Data as DashboardOverviewDto ?? new(),
                    RevenueChart = revenueTask.Result.Data as List<RevenueDataPoint> ?? new(),
                    TopRooms = topRoomsTask.Result.Data as List<TopRoomDto> ?? new(),
                    TopCustomers = topCustomersTask.Result.Data as List<TopCustomerDto> ?? new(),
                    RecentBookings = recentBookingsTask.Result.Data as List<RecentBookingDto> ?? new(),
                    Alerts = alertsTask.Result.Data as List<SystemAlertDto> ?? new(),
                    LiveOccupancy = liveOccupancyTask.Result.Data as LiveOccupancyDto ?? new(),
                    TodayStats = todayBookingsTask.Result.Data as TodayBookingsDto ?? new()
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

        #region Helper Methods

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date,
                System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private static DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            var firstMonday = jan1.AddDays(daysOffset);
            var firstWeek = GetWeekOfYear(jan1) == 1 ? firstMonday : firstMonday.AddDays(7);
            return firstWeek.AddDays((weekOfYear - 1) * 7);
        }

        #endregion
    }
}
