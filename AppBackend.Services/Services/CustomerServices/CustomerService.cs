using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CustomerModel;
using AppBackend.Services.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.CustomerServices
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HotelManagementContext _context;
        private readonly CommonCodeHelper _commonCodeHelper;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            IUnitOfWork unitOfWork,
            HotelManagementContext context,
            CommonCodeHelper commonCodeHelper,
            ILogger<CustomerService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _commonCodeHelper = commonCodeHelper;
            _logger = logger;
        }

        public async Task<ResultModel> GetCustomerListAsync(GetCustomerListRequest request)
        {
            var query = _context.Customers
                .Include(c => c.Account)
                .AsQueryable();

            if (request.FromDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(c =>
                    c.FullName.ToLower().Contains(searchLower) ||
                    (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(searchLower)) ||
                    (c.IdentityCard != null && c.IdentityCard.ToLower().Contains(searchLower)) ||
                    (c.Account != null && c.Account.Email.ToLower().Contains(searchLower)));
            }

            if (request.IsLocked.HasValue)
            {
                query = query.Where(c => c.Account != null && c.Account.IsLocked == request.IsLocked.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortDesc
                    ? query.OrderByDescending(c => EF.Property<object>(c, request.SortBy))
                    : query.OrderBy(c => EF.Property<object>(c, request.SortBy));
            }
            else
            {
                query = query.OrderByDescending(c => c.CreatedAt);
            }

            var totalRecords = await query.CountAsync();
            var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
            var skip = (pageIndex - 1) * request.PageSize;

            var customers = await query
                .Skip(skip)
                .Take(request.PageSize)
                .ToListAsync();

            var customerIds = customers.Select(c => c.CustomerId).ToList();
            var bookingAggregates = await _context.Bookings
                .Where(b => customerIds.Contains(b.CustomerId))
                .GroupBy(b => b.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalBookings = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalAmount),
                    LastBookingDate = g.Max(b => (DateTime?)b.CreatedAt)
                })
                .ToListAsync();

            var bookingLookup = bookingAggregates.ToDictionary(b => b.CustomerId, b => b);

            var items = customers.Select(c =>
            {
                bookingLookup.TryGetValue(c.CustomerId, out var stats);
                return new CustomerListItemDto
                {
                    CustomerId = c.CustomerId,
                    AccountId = c.AccountId,
                    FullName = c.FullName,
                    Email = c.Account?.Email ?? string.Empty,
                    PhoneNumber = c.PhoneNumber,
                    IsLocked = c.Account?.IsLocked ?? false,
                    TotalBookings = stats?.TotalBookings ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0,
                    LastBookingDate = stats?.LastBookingDate,
                    CreatedAt = c.CreatedAt
                };
            }).ToList();

            var pagedResponse = new PagedResponseDto<CustomerListItemDto>
            {
                Items = items,
                TotalCount = totalRecords,
                PageIndex = pageIndex,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = pagedResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCustomerDetailAsync(int customerId)
        {
            var customer = await _unitOfWork.Customers.GetSingleAsync(
                c => c.CustomerId == customerId,
                c => c.Account);

            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Khách hàng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            try
            {
                var accountRoles = customer.AccountId.HasValue
                    ? await _unitOfWork.Accounts.GetRoleNamesByAccountIdAsync(customer.AccountId.Value)
                    : new List<string>();

                string? avatarUrl = null;
                if (customer.AvatarMediaId.HasValue)
                {
                    var avatar = await _unitOfWork.Mediums.GetByIdAsync(customer.AvatarMediaId.Value);
                    avatarUrl = avatar?.FilePath;
                }

                var bookingQuery = _context.Bookings.Where(b => b.CustomerId == customerId);
                var totalBookings = await bookingQuery.CountAsync();
                var lastBookingDate = await bookingQuery
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => (DateTime?)b.CreatedAt)
                    .FirstOrDefaultAsync();
                var totalSpent = await bookingQuery.SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
                var upcomingBookings = await bookingQuery.CountAsync(b => b.CheckInDate >= DateTime.UtcNow);

                var completedStatusId = await _commonCodeHelper.GetCommonCodeIdByNameAsync("BookingStatus", "Completed");
                var cancelledStatusId = await _commonCodeHelper.GetCommonCodeIdByNameAsync("BookingStatus", "Cancelled");

                var completedBookings = completedStatusId.HasValue
                    ? await bookingQuery.CountAsync(b => b.StatusId == completedStatusId.Value)
                    : 0;
                var cancelledBookings = cancelledStatusId.HasValue
                    ? await bookingQuery.CountAsync(b => b.StatusId == cancelledStatusId.Value)
                    : 0;

                var bookingIds = await bookingQuery.Select(b => b.BookingId).ToListAsync();
                var totalTransactions = bookingIds.Any()
                    ? await _context.Transactions.CountAsync(t => bookingIds.Contains(t.BookingId))
                    : 0;
                var totalPaidAmount = bookingIds.Any()
                    ? await _context.Transactions
                        .Where(t => bookingIds.Contains(t.BookingId))
                        .SumAsync(t => (decimal?)t.PaidAmount) ?? 0
                    : 0;

                var totalFeedbacks = customer.AccountId.HasValue
                    ? await _context.Comments.CountAsync(c => c.AccountId == customer.AccountId.Value)
                    : 0;

                var recentBookings = await _context.Bookings
                    .Where(b => b.CustomerId == customerId)
                    .Include(b => b.Status)
                    .Include(b => b.BookingType)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new CustomerBookingBrief
                    {
                        BookingId = b.BookingId,
                        StatusCode = b.Status != null ? b.Status.CodeName : string.Empty,
                        StatusName = b.Status != null ? b.Status.CodeValue : string.Empty,
                        BookingType = b.BookingType != null ? b.BookingType.CodeName : string.Empty,
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        TotalAmount = b.TotalAmount,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                var response = new CustomerDetailResponse
                {
                    BasicInfo = new CustomerBasicInfo
                    {
                        CustomerId = customer.CustomerId,
                        AccountId = customer.AccountId,
                        FullName = customer.FullName,
                        Email = customer.Account?.Email ?? string.Empty,
                        PhoneNumber = customer.PhoneNumber,
                        IdentityCard = customer.IdentityCard,
                        Address = customer.Address,
                        AvatarUrl = avatarUrl,
                        CreatedAt = customer.CreatedAt
                    },
                    Account = customer.Account != null
                        ? new AccountSnapshot
                        {
                            AccountId = customer.Account.AccountId,
                            Username = customer.Account.Username,
                            Email = customer.Account.Email,
                            IsLocked = customer.Account.IsLocked,
                            LastLoginAt = customer.Account.LastLoginAt,
                            Roles = accountRoles
                        }
                        : null,
                    Statistics = new CustomerStatistics
                    {
                        TotalBookings = totalBookings,
                        CompletedBookings = completedBookings,
                        CancelledBookings = cancelledBookings,
                        UpcomingBookings = upcomingBookings,
                        TotalSpent = totalSpent,
                        LastBookingDate = lastBookingDate,
                        TotalFeedbacks = totalFeedbacks,
                        TotalTransactions = totalTransactions,
                        TotalPaidAmount = totalPaidAmount
                    },
                    RecentBookings = recentBookings
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = CommonMessageConstants.GET_SUCCESS,
                    Data = response,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer detail for {CustomerId}", customerId);
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Có lỗi xảy ra khi lấy thông tin khách hàng"
                };
            }
        }

        public async Task<ResultModel> BanCustomerAsync(BanCustomerRequest request)
        {
            var customer = await _unitOfWork.Customers.GetSingleAsync(
                c => c.CustomerId == request.CustomerId,
                c => c.Account);

            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Khách hàng"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (customer.Account == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "Khách hàng chưa có tài khoản để khoá/mở khoá",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            customer.Account.IsLocked = request.IsLocked;
            customer.Account.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Accounts.UpdateAsync(customer.Account);
            await _unitOfWork.SaveChangesAsync();

            var message = request.IsLocked
                ? "Khoá tài khoản khách hàng thành công"
                : "Mở khoá tài khoản khách hàng thành công";

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = message,
                Data = new { customer.CustomerId, IsLocked = customer.Account.IsLocked },
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
