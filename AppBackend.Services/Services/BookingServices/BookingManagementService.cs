using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.BookingServices
{
    /// <summary>
    /// Implementation cho quản lý booking offline - Lễ tân, Manager, Admin
    /// </summary>
    public class BookingManagementService : IBookingManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public BookingManagementService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        /// <summary>
        /// Tìm customer theo email hoặc số điện thoại
        /// </summary>
        public async Task<ResultModel> SearchCustomerAsync(string searchTerm)
        {
            try
            {
                searchTerm = searchTerm.Trim();

                // Tìm theo email hoặc số điện thoại
                var customers = await _unitOfWork.Customers.FindAsync(c =>
                    (c.Account != null && c.Account.Email == searchTerm) ||
                    c.PhoneNumber == searchTerm);

                var customer = customers.FirstOrDefault();

                if (customer == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = true,
                        StatusCode = StatusCodes.Status200OK,
                        Data = null,
                        Message = "Không tìm thấy khách hàng"
                    };
                }

                // Lấy thông tin booking history
                var bookingsList = (await _unitOfWork.Bookings.FindAsync(b => b.CustomerId == customer.CustomerId)).ToList();
                var lastBooking = bookingsList.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

                var customerDto = new CustomerInfoDto
                {
                    CustomerId = customer.CustomerId,
                    FullName = customer.FullName,
                    Email = customer.Account?.Email ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    IdentityCard = customer.IdentityCard,
                    Address = customer.Address,
                    TotalBookings = bookingsList.Count,
                    LastBookingDate = lastBooking?.CreatedAt
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = customerDto,
                    Message = "Tìm thấy thông tin khách hàng"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi tìm kiếm: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Tạo booking offline
        /// </summary>
        public Task<ResultModel> CreateOfflineBookingAsync(CreateOfflineBookingRequest request, int employeeId)
        {
            try
            {
                // TODO: Implement tạo booking offline
                // 1. Tìm hoặc tạo customer
                // 2. Check room availability
                // 3. Tạo booking
                // 4. Tạo transaction (deposit)
                // 5. Gửi email xác nhận

                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi tạo booking: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Cập nhật booking offline
        /// </summary>
        public Task<ResultModel> UpdateOfflineBookingAsync(int bookingId, UpdateOfflineBookingRequest request, int employeeId)
        {
            try
            {
                // TODO: Implement cập nhật booking
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi cập nhật: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Xác nhận đặt cọc offline
        /// </summary>
        public Task<ResultModel> ConfirmOfflineDepositAsync(int bookingId, ConfirmOfflineDepositRequest request, int employeeId)
        {
            try
            {
                // TODO: Implement xác nhận đặt cọc
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi xác nhận đặt cọc: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Xác nhận thanh toán toàn bộ và gửi email cảm ơn
        /// </summary>
        public Task<ResultModel> ConfirmOfflinePaymentAsync(int bookingId, ConfirmOfflinePaymentRequest request, int employeeId)
        {
            try
            {
                // TODO: Implement xác nhận thanh toán và gửi email
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi xác nhận thanh toán: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lấy danh sách booking offline
        /// </summary>
        public Task<ResultModel> GetOfflineBookingsAsync(OfflineBookingFilterRequest filter)
        {
            try
            {
                // TODO: Implement lấy danh sách booking với filter
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi lấy danh sách: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Hủy booking offline
        /// </summary>
        public Task<ResultModel> CancelOfflineBookingAsync(int bookingId, string reason, int employeeId)
        {
            try
            {
                // TODO: Implement hủy booking
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi hủy booking: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Gửi lại email xác nhận
        /// </summary>
        public Task<ResultModel> ResendBookingConfirmationEmailAsync(int bookingId)
        {
            try
            {
                // TODO: Implement gửi lại email
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status501NotImplemented,
                    Message = "Chức năng đang được phát triển"
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi gửi email: {ex.Message}"
                });
            }
        }
    }
}
