using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;
using AppBackend.BusinessObjects.Models;
using AppBackend.Services.Helpers;

namespace AppBackend.Services.Services.BookingServices
{
    /// <summary>
    /// Implementation cho quản lý booking offline - Lễ tân, Manager, Admin
    /// </summary>
    public class BookingManagementService : IBookingManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly CacheHelper _cacheHelper;

        public BookingManagementService(IUnitOfWork unitOfWork, IEmailService emailService, CacheHelper cacheHelper)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _cacheHelper = cacheHelper;
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
        public async Task<ResultModel> CreateOfflineBookingAsync(CreateOfflineBookingRequest request, int employeeId)
        {
            try
            {
                // 1. Tìm hoặc tạo customer
                var customer = await FindOrCreateCustomerAsync(request.Email, request.PhoneNumber, request.FullName, 
                    request.IdentityCard, request.Address);

                if (customer == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "Không thể tạo thông tin khách hàng"
                    };
                }

                // 2. Validate RoomTypes
                if (request.RoomTypes == null || !request.RoomTypes.Any())
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Vui lòng chọn ít nhất một loại phòng"
                    };
                }

                // 3. Generate unique lock ID
                var lockId = Guid.NewGuid().ToString();

                // 4. Tìm và lock phòng available
                var selectedRooms = new List<Room>();
                var lockedRoomIds = new List<int>();
                
                foreach (var roomTypeRequest in request.RoomTypes)
                {
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeRequest.RoomTypeId);
                    if (roomType == null)
                    {
                        ReleaseAllLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);
                        return new ResultModel
                        {
                            IsSuccess = false,
                            StatusCode = StatusCodes.Status404NotFound,
                            Message = $"Loại phòng ID {roomTypeRequest.RoomTypeId} không tồn tại"
                        };
                    }

                    var availableRooms = await FindAvailableRoomsByTypeAsync(
                        roomTypeRequest.RoomTypeId, 
                        roomTypeRequest.Quantity, 
                        request.CheckInDate, 
                        request.CheckOutDate);

                    if (availableRooms.Count < roomTypeRequest.Quantity)
                    {
                        ReleaseAllLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);
                        return new ResultModel
                        {
                            IsSuccess = false,
                            StatusCode = StatusCodes.Status409Conflict,
                            Message = $"Không đủ phòng {roomType.TypeName}. Yêu cầu: {roomTypeRequest.Quantity}, Còn: {availableRooms.Count}"
                        };
                    }

                    // Lock các phòng
                    foreach (var room in availableRooms.Take(roomTypeRequest.Quantity))
                    {
                        var lockKey = $"{room.RoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                        var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

                        if (!locked)
                        {
                            ReleaseAllLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);
                            return new ResultModel
                            {
                                IsSuccess = false,
                                StatusCode = StatusCodes.Status409Conflict,
                                Message = $"Phòng {room.RoomName} đang được đặt bởi người khác"
                            };
                        }

                        lockedRoomIds.Add(room.RoomId);
                        selectedRooms.Add(room);
                    }
                }

                // 5. Tính tổng tiền
                decimal totalAmount = 0;
                var numberOfNights = (request.CheckOutDate - request.CheckInDate).Days;

                foreach (var room in selectedRooms)
                {
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                    var roomSubTotal = await CalculateRoomPriceAsync(room.RoomId, request.CheckInDate, request.CheckOutDate, roomType.BasePriceNight);
                    totalAmount += roomSubTotal;
                }

                // 6. Lấy BookingType = "Walkin" (booking offline)
                var walkinBookingType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeName == "Walkin")).FirstOrDefault();

                var unpaidPaymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();

                var unpaidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "DepositStatus" && c.CodeName == "Unpaid")).FirstOrDefault();

                // 7. Tạo Booking
                var booking = new Booking
                {
                    CustomerId = customer.CustomerId,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalAmount = totalAmount,
                    DepositAmount = request.DepositAmount,
                    PaymentStatusId = unpaidPaymentStatus?.CodeId,
                    DepositStatusId = request.DepositAmount > 0 ? 
                        (await _unitOfWork.CommonCodes.FindAsync(c => c.CodeType == "DepositStatus" && c.CodeName == "Paid")).FirstOrDefault()?.CodeId :
                        unpaidDepositStatus?.CodeId,
                    BookingTypeId = walkinBookingType?.CodeId,
                    SpecialRequests = request.SpecialRequests,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = employeeId
                };

                await _unitOfWork.Bookings.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // 8. Tạo BookingRooms
                foreach (var room in selectedRooms)
                {
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                    var roomPrice = await CalculateRoomPriceAsync(room.RoomId, request.CheckInDate, request.CheckOutDate, roomType.BasePriceNight);

                    var bookingRoom = new BookingRoom
                    {
                        BookingId = booking.BookingId,
                        RoomId = room.RoomId,
                        PricePerNight = roomType.BasePriceNight,
                        SubTotal = roomPrice
                    };

                    await _unitOfWork.BookingRooms.AddAsync(bookingRoom);
                }

                // 9. Tạo Transaction cho deposit (nếu có)
                if (request.DepositAmount > 0)
                {
                    var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentMethod" && c.CodeName == request.PaymentMethod)).FirstOrDefault();

                    var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

                    var transaction = new BusinessObjects.Models.Transaction
                    {
                        BookingId = booking.BookingId,
                        TotalAmount = request.DepositAmount,
                        DepositAmount = request.DepositAmount,
                        PaymentMethodId = paymentMethod?.CodeId ?? 0,
                        PaymentStatusId = completedStatus?.CodeId ?? 0,
                        TransactionStatusId = completedStatus?.CodeId ?? 0,
                        TransactionRef = $"OFFLINE-DEPOSIT-{booking.BookingId}",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = employeeId
                    };

                    await _unitOfWork.Transactions.AddAsync(transaction);
                }

                await _unitOfWork.SaveChangesAsync();

                // 10. Release locks sau khi booking thành công
                ReleaseAllLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);

                // 11. Gửi email xác nhận
                try
                {
                    await SendBookingConfirmationEmailAsync(booking.BookingId);
                }
                catch (Exception emailEx)
                {
                    // Log error nhưng không fail request
                    Console.WriteLine($"Failed to send email: {emailEx.Message}");
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status201Created,
                    Data = new { BookingId = booking.BookingId },
                    Message = "Tạo booking offline thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi tạo booking: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cập nhật booking offline
        /// </summary>
        public async Task<ResultModel> UpdateOfflineBookingAsync(int bookingId, UpdateOfflineBookingRequest request, int employeeId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Kiểm tra booking type phải là Walkin
                var walkinType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeName == "Walkin")).FirstOrDefault();

                if (booking.BookingTypeId != walkinType?.CodeId)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Chỉ có thể cập nhật booking offline"
                    };
                }

                // Kiểm tra booking chưa check-in
                if (booking.CheckInDate <= DateTime.UtcNow)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Không thể cập nhật booking đã check-in"
                    };
                }

                // Cập nhật thông tin customer
                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                if (customer != null)
                {
                    if (!string.IsNullOrEmpty(request.FullName))
                        customer.FullName = request.FullName;
                    if (!string.IsNullOrEmpty(request.PhoneNumber))
                        customer.PhoneNumber = request.PhoneNumber;
                    if (!string.IsNullOrEmpty(request.IdentityCard))
                        customer.IdentityCard = request.IdentityCard;
                    if (!string.IsNullOrEmpty(request.Address))
                        customer.Address = request.Address;

                    await _unitOfWork.Customers.UpdateAsync(customer);
                }

                // Cập nhật booking
                if (request.CheckInDate.HasValue)
                    booking.CheckInDate = request.CheckInDate.Value;
                if (request.CheckOutDate.HasValue)
                    booking.CheckOutDate = request.CheckOutDate.Value;
                if (!string.IsNullOrEmpty(request.SpecialRequests))
                    booking.SpecialRequests = request.SpecialRequests;

                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = employeeId;

                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Cập nhật booking thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi cập nhật: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xác nhận đặt cọc offline
        /// </summary>
        public async Task<ResultModel> ConfirmOfflineDepositAsync(int bookingId, ConfirmOfflineDepositRequest request, int employeeId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Cập nhật deposit amount
                booking.DepositAmount = request.DepositAmount;
                
                var paidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "DepositStatus" && c.CodeName == "Paid")).FirstOrDefault();
                booking.DepositStatusId = paidDepositStatus?.CodeId;

                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = employeeId;

                // Tạo transaction cho deposit
                var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.CodeName == request.PaymentMethod)).FirstOrDefault();

                var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

                var transaction = new Transaction
                {
                    BookingId = booking.BookingId,
                    TotalAmount = request.DepositAmount,
                    DepositAmount = request.DepositAmount,
                    PaymentMethodId = paymentMethod?.CodeId ?? 0,
                    PaymentStatusId = completedStatus?.CodeId ?? 0,
                    TransactionRef = request.TransactionReference ?? $"OFFLINE-DEPOSIT-{booking.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    PaidAmount = request.DepositAmount,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = employeeId
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Xác nhận đặt cọc thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi xác nhận đặt cọc: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xác nhận thanh toán toàn bộ và gửi email cảm ơn
        /// </summary>
        public async Task<ResultModel> ConfirmOfflinePaymentAsync(int bookingId, ConfirmOfflinePaymentRequest request, int employeeId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Cập nhật payment status
                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeName == "Paid")).FirstOrDefault();
                booking.PaymentStatusId = paidStatus?.CodeId;

                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = employeeId;

                // Tạo transaction cho payment
                var paymentMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentMethod" && c.CodeName == request.PaymentMethod)).FirstOrDefault();

                var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

                var transaction = new Transaction
                {
                    BookingId = booking.BookingId,
                    TotalAmount = request.PaidAmount,
                    PaymentMethodId = paymentMethod?.CodeId ?? 0,
                    PaymentStatusId = completedStatus?.CodeId ?? 0,
                    TransactionRef = request.TransactionReference ?? $"OFFLINE-PAYMENT-{booking.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    PaidAmount = request.PaidAmount,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = employeeId
                };

                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Gửi email cảm ơn
                try
                {
                    await SendThankYouEmailAsync(booking.BookingId);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Failed to send thank you email: {emailEx.Message}");
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Xác nhận thanh toán thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi xác nhận thanh toán: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy danh sách booking offline
        /// </summary>
        public async Task<ResultModel> GetOfflineBookingsAsync(OfflineBookingFilterRequest filter)
        {
            try
            {
                // Lấy BookingType Walkin
                var walkinType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeName == "Walkin")).FirstOrDefault();

                if (walkinType == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy BookingType Walkin"
                    };
                }

                // Query bookings
                var query = await _unitOfWork.Bookings.FindAsync(b => b.BookingTypeId == walkinType.CodeId);
                var bookings = query.ToList();

                // Apply filters
                if (filter.FromDate.HasValue)
                    bookings = bookings.Where(b => b.CheckInDate >= filter.FromDate.Value).ToList();

                if (filter.ToDate.HasValue)
                    bookings = bookings.Where(b => b.CheckOutDate <= filter.ToDate.Value).ToList();

                if (!string.IsNullOrEmpty(filter.CustomerName))
                {
                    bookings = bookings.Where(b => 
                        b.Customer.FullName.Contains(filter.CustomerName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.PhoneNumber))
                {
                    bookings = bookings.Where(b => 
                        b.Customer.PhoneNumber != null && 
                        b.Customer.PhoneNumber.Contains(filter.PhoneNumber)).ToList();
                }

                if (!string.IsNullOrEmpty(filter.PaymentStatus))
                {
                    var paymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentStatus" && c.CodeName == filter.PaymentStatus)).FirstOrDefault();
                    if (paymentStatus != null)
                        bookings = bookings.Where(b => b.PaymentStatusId == paymentStatus.CodeId).ToList();
                }

                if (!string.IsNullOrEmpty(filter.DepositStatus))
                {
                    var depositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "DepositStatus" && c.CodeName == filter.DepositStatus)).FirstOrDefault();
                    if (depositStatus != null)
                        bookings = bookings.Where(b => b.DepositStatusId == depositStatus.CodeId).ToList();
                }

                // Pagination
                var totalRecords = bookings.Count;
                var pagedBookings = bookings
                    .OrderByDescending(b => b.CreatedAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();

                // Map to DTOs
                var bookingDtos = new List<OfflineBookingDto>();
                foreach (var booking in pagedBookings)
                {
                    var bookingDto = await MapToOfflineBookingDto(booking);
                    bookingDtos.Add(bookingDto);
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize,
                        TotalPages = (int)Math.Ceiling(totalRecords / (double)filter.PageSize),
                        Bookings = bookingDtos
                    },
                    Message = "Lấy danh sách booking thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi lấy danh sách: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Hủy booking offline
        /// </summary>
        public async Task<ResultModel> CancelOfflineBookingAsync(int bookingId, string reason, int employeeId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Kiểm tra booking chưa check-in
                if (booking.CheckInDate <= DateTime.UtcNow)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Không thể hủy booking đã check-in"
                    };
                }

                // Cập nhật status
                var cancelledStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "Status" && c.CodeName == "Cancelled")).FirstOrDefault();

                // Update payment status to cancelled
                booking.PaymentStatusId = cancelledStatus?.CodeId;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = employeeId;
                booking.SpecialRequests = (booking.SpecialRequests ?? "") + $"\n[CANCELLED] Reason: {reason}";

                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Release room locks if any
                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                foreach (var br in bookingRooms)
                {
                    var lockKey = $"{br.RoomId}_{booking.CheckInDate:yyyyMMdd}_{booking.CheckOutDate:yyyyMMdd}";
                    // Try to release lock without checking lockId owner
                    try
                    {
                        _cacheHelper.Remove(CachePrefix.RoomBookingLock, lockKey);
                    }
                    catch
                    {
                        // Ignore if lock doesn't exist
                    }
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Hủy booking thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi hủy booking: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gửi lại email xác nhận
        /// </summary>
        public async Task<ResultModel> ResendBookingConfirmationEmailAsync(int bookingId)
        {
            try
            {
                await SendBookingConfirmationEmailAsync(bookingId);

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Đã gửi lại email xác nhận"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi gửi email: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy danh sách booking với filter và phân trang
        /// </summary>
        public async Task<ResultModel<PagedBookingListResponse>> GetBookingListAsync(GetBookingListRequest request)
        {
            try
            {
                // Lấy tất cả bookings
                var query = (await _unitOfWork.Bookings.GetAllAsync()).AsQueryable();

                // Apply filters
                if (request.FromDate.HasValue)
                {
                    query = query.Where(b => b.CheckInDate >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    query = query.Where(b => b.CheckOutDate <= request.ToDate.Value);
                }

                if (!string.IsNullOrEmpty(request.BookingStatus))
                {
                    var statusCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "BookingStatus" && c.CodeValue == request.BookingStatus)).FirstOrDefault();
                    if (statusCode != null)
                        query = query.Where(b => b.PaymentStatusId == statusCode.CodeId);
                }

                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    var paymentCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "PaymentStatus" && c.CodeValue == request.PaymentStatus)).FirstOrDefault();
                    if (paymentCode != null)
                        query = query.Where(b => b.PaymentStatusId == paymentCode.CodeId);
                }

                if (!string.IsNullOrEmpty(request.DepositStatus))
                {
                    var depositCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "DepositStatus" && c.CodeValue == request.DepositStatus)).FirstOrDefault();
                    if (depositCode != null)
                        query = query.Where(b => b.DepositStatusId == depositCode.CodeId);
                }

                if (!string.IsNullOrEmpty(request.BookingType))
                {
                    var typeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "BookingType" && c.CodeValue == request.BookingType)).FirstOrDefault();
                    if (typeCode != null)
                        query = query.Where(b => b.BookingTypeId == typeCode.CodeId);
                }

                // Search filters
                if (!string.IsNullOrEmpty(request.CustomerName))
                {
                    var customers = await _unitOfWork.Customers.FindAsync(c =>
                        c.FullName.Contains(request.CustomerName));
                    var customerIds = customers.Select(c => c.CustomerId).ToList();
                    query = query.Where(b => customerIds.Contains(b.CustomerId));
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    var customers = await _unitOfWork.Customers.FindAsync(c =>
                        c.PhoneNumber != null && c.PhoneNumber.Contains(request.PhoneNumber));
                    var customerIds = customers.Select(c => c.CustomerId).ToList();
                    query = query.Where(b => customerIds.Contains(b.CustomerId));
                }

                if (!string.IsNullOrEmpty(request.Email))
                {
                    var accounts = await _unitOfWork.Accounts.FindAsync(a =>
                        a.Email.Contains(request.Email));
                    var accountIds = accounts.Select(a => a.AccountId).ToList();
                    var customers = await _unitOfWork.Customers.FindAsync(c =>
                        c.AccountId.HasValue && accountIds.Contains(c.AccountId.Value));
                    var customerIds = customers.Select(c => c.CustomerId).ToList();
                    query = query.Where(b => customerIds.Contains(b.CustomerId));
                }

                // Sorting
                query = request.SortBy?.ToLower() switch
                {
                    "checkindate" => request.IsDescending ? 
                        query.OrderByDescending(b => b.CheckInDate) : query.OrderBy(b => b.CheckInDate),
                    "totalamount" => request.IsDescending ? 
                        query.OrderByDescending(b => b.TotalAmount) : query.OrderBy(b => b.TotalAmount),
                    _ => request.IsDescending ? 
                        query.OrderByDescending(b => b.CreatedAt) : query.OrderBy(b => b.CreatedAt)
                };

                // Count total
                var totalRecords = query.Count();

                // Pagination
                var bookings = query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // Map to DTOs
                var items = new List<BookingListItemDto>();
                foreach (var booking in bookings)
                {
                    var item = await MapToBookingListItemDto(booking);
                    items.Add(item);
                }

                var response = new PagedBookingListResponse
                {
                    Items = items,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize),
                    HasPreviousPage = request.PageNumber > 1,
                    HasNextPage = request.PageNumber < (int)Math.Ceiling(totalRecords / (double)request.PageSize)
                };

                return new ResultModel<PagedBookingListResponse>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = response,
                    Message = "Lấy danh sách booking thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<PagedBookingListResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy chi tiết đầy đủ của một booking
        /// </summary>
        public async Task<ResultModel<BookingDetailDto>> GetBookingDetailAsync(int bookingId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel<BookingDetailDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                var detail = await MapToBookingDetailDto(booking);

                return new ResultModel<BookingDetailDto>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = detail,
                    Message = "Lấy chi tiết booking thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<BookingDetailDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cập nhật trạng thái booking
        /// </summary>
        public async Task<ResultModel> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusRequest request, int employeeId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Lấy status code từ CommonCode
                var statusCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && c.CodeValue == request.Status)).FirstOrDefault();

                if (statusCode == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Trạng thái không hợp lệ"
                    };
                }

                booking.PaymentStatusId = statusCode.CodeId;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = employeeId;

                if (!string.IsNullOrEmpty(request.Note))
                {
                    booking.SpecialRequests = (booking.SpecialRequests ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {request.Note}";
                }

                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Cập nhật trạng thái thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy thống kê booking
        /// </summary>
        public async Task<ResultModel<BookingStatisticsResponse>> GetBookingStatisticsAsync(BookingStatisticsRequest request)
        {
            try
            {
                var query = (await _unitOfWork.Bookings.GetAllAsync()).AsQueryable();

                if (request.FromDate.HasValue)
                    query = query.Where(b => b.CreatedAt >= request.FromDate.Value);

                if (request.ToDate.HasValue)
                    query = query.Where(b => b.CreatedAt <= request.ToDate.Value);

                var bookings = query.ToList();

                var onlineType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeValue == "Online")).FirstOrDefault();
                var walkinType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingType" && c.CodeValue == "Walkin")).FirstOrDefault();
                var confirmedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && c.CodeValue == "Confirmed")).FirstOrDefault();
                var cancelledStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && c.CodeValue == "Cancelled")).FirstOrDefault();

                var statistics = new BookingStatisticsResponse
                {
                    TotalBookings = bookings.Count,
                    TotalOnlineBookings = onlineType != null ? bookings.Count(b => b.BookingTypeId == onlineType.CodeId) : 0,
                    TotalWalkinBookings = walkinType != null ? bookings.Count(b => b.BookingTypeId == walkinType.CodeId) : 0,
                    TotalConfirmedBookings = confirmedStatus != null ? bookings.Count(b => b.PaymentStatusId == confirmedStatus.CodeId) : 0,
                    TotalCancelledBookings = cancelledStatus != null ? bookings.Count(b => b.PaymentStatusId == cancelledStatus.CodeId) : 0,
                    TotalRevenue = bookings.Sum(b => b.TotalAmount),
                    TotalDeposit = bookings.Sum(b => b.DepositAmount),
                    AverageBookingValue = bookings.Any() ? bookings.Average(b => b.TotalAmount) : 0
                };

                // Group by period
                if (!string.IsNullOrEmpty(request.GroupBy))
                {
                    var grouped = request.GroupBy.ToLower() switch
                    {
                        "day" => bookings.GroupBy(b => b.CreatedAt.Date),
                        "week" => bookings.GroupBy(b => b.CreatedAt.Date.AddDays(-(int)b.CreatedAt.DayOfWeek)),
                        "month" => bookings.GroupBy(b => new DateTime(b.CreatedAt.Year, b.CreatedAt.Month, 1)),
                        "year" => bookings.GroupBy(b => new DateTime(b.CreatedAt.Year, 1, 1)),
                        _ => bookings.GroupBy(b => b.CreatedAt.Date)
                    };

                    statistics.StatisticsByPeriod = grouped.Select(g => new BookingStatisticsItemDto
                    {
                        Date = g.Key,
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        TotalBookings = g.Count(),
                        TotalRevenue = g.Sum(b => b.TotalAmount),
                        AverageBookingValue = g.Average(b => b.TotalAmount)
                    }).ToList();
                }

                return new ResultModel<BookingStatisticsResponse>
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = statistics,
                    Message = "Lấy thống kê thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<BookingStatisticsResponse>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Hủy booking
        /// </summary>
        public async Task<ResultModel> CancelBookingAsync(int bookingId, CancelBookingRequest request, int employeeId)
        {
            return await CancelOfflineBookingAsync(bookingId, request.Reason ?? "No reason provided", employeeId);
        }

        // ===== PRIVATE HELPER METHODS =====

        private async Task<Customer?> FindOrCreateCustomerAsync(string email, string phoneNumber, string fullName, 
            string? identityCard, string? address)
        {
            // Tìm customer theo email hoặc phone
            var existingCustomers = await _unitOfWork.Customers.FindAsync(c =>
                (c.Account != null && c.Account.Email == email) ||
                c.PhoneNumber == phoneNumber);

            var customer = existingCustomers.FirstOrDefault();

            if (customer != null)
            {
                // Update thông tin nếu cần
                customer.FullName = fullName;
                customer.PhoneNumber = phoneNumber;
                if (!string.IsNullOrEmpty(identityCard))
                    customer.IdentityCard = identityCard;
                if (!string.IsNullOrEmpty(address))
                    customer.Address = address;

                await _unitOfWork.Customers.UpdateAsync(customer);
                await _unitOfWork.SaveChangesAsync();
                return customer;
            }

            // Tạo customer mới (không có account)
            customer = new Customer
            {
                FullName = fullName,
                PhoneNumber = phoneNumber,
                IdentityCard = identityCard,
                Address = address,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return customer;
        }

        private async Task<List<Room>> FindAvailableRoomsByTypeAsync(int roomTypeId, int quantity, 
            DateTime checkInDate, DateTime checkOutDate)
        {
            var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            var availableRooms = new List<Room>();

            foreach (var room in allRoomsOfType)
            {
                if (availableRooms.Count >= quantity) break;

                if (await IsRoomAvailableAsync(room.RoomId, checkInDate, checkOutDate))
                {
                    availableRooms.Add(room);
                }
            }

            return availableRooms;
        }

        private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate)
        {
            // Check cache lock
            var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);

            if (!string.IsNullOrEmpty(lockedBy))
                return false;

            // Check database bookings
            var existingBookings = (await _unitOfWork.BookingRooms.FindAsync(br =>
                br.RoomId == roomId &&
                br.Booking.CheckInDate < checkOutDate &&
                br.Booking.CheckOutDate > checkInDate)).ToList();

            if (!existingBookings.Any())
                return true;

            // Check if any booking has completed transaction
            var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

            if (completedStatus == null) return true;

            foreach (var bookingRoom in existingBookings)
            {
                var transactions = await _unitOfWork.Transactions.FindAsync(t => 
                    t.BookingId == bookingRoom.BookingId);

                if (transactions.Any(t => t.TransactionStatusId == completedStatus.CodeId))
                    return false;
            }

            return true;
        }

        private async Task<decimal> CalculateRoomPriceAsync(int roomId, DateTime checkInDate, 
            DateTime checkOutDate, decimal basePricePerNight)
        {
            decimal totalPrice = 0;
            var numberOfNights = (checkOutDate - checkInDate).Days;

            for (int night = 0; night < numberOfNights; night++)
            {
                var nightDate = checkInDate.AddDays(night);
                var priceForNight = basePricePerNight;

                var holidayPricing = (await _unitOfWork.HolidayPricings.FindAsync(hp =>
                    hp.IsActive &&
                    hp.RoomId == roomId &&
                    hp.StartDate <= nightDate &&
                    hp.EndDate >= nightDate
                )).FirstOrDefault();

                if (holidayPricing != null)
                {
                    priceForNight = basePricePerNight + holidayPricing.PriceAdjustment;
                }

                totalPrice += priceForNight;
            }

            return totalPrice;
        }

        private void ReleaseAllLocks(List<int> roomIds, DateTime checkInDate, DateTime checkOutDate, string lockId)
        {
            foreach (var roomId in roomIds)
            {
                var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
                _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
            }
        }

        private async Task<OfflineBookingDto> MapToOfflineBookingDto(Booking booking)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId);
            var transactions = (await _unitOfWork.Transactions.FindAsync(t => t.BookingId == booking.BookingId)).ToList();

            var roomDtos = new List<RoomDto>();
            foreach (var br in bookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(br.RoomId);
                if (room == null) continue;
                
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                if (roomType == null) continue;

                roomDtos.Add(new RoomDto
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomName,
                    RoomTypeName = roomType.TypeName,
                    PricePerNight = br.PricePerNight
                });
            }

            var paymentHistory = new List<PaymentHistoryDto>();
            foreach (var trans in transactions)
            {
                CommonCode? paymentMethod = null;
                if (trans.PaymentMethodId > 0)
                {
                    paymentMethod = await _unitOfWork.CommonCodes.GetByIdAsync(trans.PaymentMethodId);
                }

                Employee? employee = null;
                if (trans.CreatedBy.HasValue && trans.CreatedBy.Value > 0)
                {
                    employee = await _unitOfWork.Employees.GetByIdAsync(trans.CreatedBy.Value);
                }

                paymentHistory.Add(new PaymentHistoryDto
                {
                    TransactionId = trans.TransactionId,
                    Amount = trans.TotalAmount,
                    PaymentMethod = paymentMethod?.CodeValue ?? "N/A",
                    TransactionType = trans.DepositAmount.HasValue ? "Deposit" : "FullPayment",
                    Note = trans.TransactionRef,
                    ProcessedBy = employee?.FullName ?? "System",
                    ProcessedAt = trans.CreatedAt
                });
            }

            CommonCode? paymentStatus = null;
            if (booking.PaymentStatusId.HasValue && booking.PaymentStatusId.Value > 0)
            {
                paymentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId.Value);
            }

            CommonCode? depositStatus = null;
            if (booking.DepositStatusId.HasValue && booking.DepositStatusId.Value > 0)
            {
                depositStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.DepositStatusId.Value);
            }

            Employee? createdByEmployee = null;
            if (booking.CreatedBy.HasValue && booking.CreatedBy.Value > 0)
            {
                createdByEmployee = await _unitOfWork.Employees.GetByIdAsync(booking.CreatedBy.Value);
            }

            if (customer == null)
            {
                throw new Exception("Customer not found");
            }

            return new OfflineBookingDto
            {
                BookingId = booking.BookingId,
                Customer = new CustomerInfoDto
                {
                    CustomerId = customer.CustomerId,
                    FullName = customer.FullName,
                    Email = customer.Account?.Email ?? "",
                    PhoneNumber = customer.PhoneNumber ?? "",
                    IdentityCard = customer.IdentityCard,
                    Address = customer.Address
                },
                Rooms = roomDtos,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalNights = (booking.CheckOutDate - booking.CheckInDate).Days,
                TotalAmount = booking.TotalAmount,
                DepositAmount = booking.DepositAmount,
                RemainingAmount = booking.TotalAmount - booking.DepositAmount,
                PaymentStatus = paymentStatus?.CodeValue ?? "N/A",
                DepositStatus = depositStatus?.CodeValue ?? "N/A",
                SpecialRequests = booking.SpecialRequests,
                CreatedByEmployee = createdByEmployee?.FullName ?? "System",
                CreatedAt = booking.CreatedAt,
                PaymentHistory = paymentHistory
            };
        }

        private async Task SendBookingConfirmationEmailAsync(int bookingId)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null) return;

            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            if (customer?.Account == null) return;

            // Use existing email service method
            await _emailService.SendBookingConfirmationEmailAsync(bookingId);
        }

        private async Task SendThankYouEmailAsync(int bookingId)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null) return;

            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            if (customer?.Account == null) return;

            // Send thank you email
            await _emailService.SendEmail(
                customer.Account.Email,
                "Cảm ơn quý khách",
                $"Cảm ơn quý khách đã sử dụng dịch vụ. Booking #{bookingId}"
            );
        }

        // ===== PRIVATE MAPPING METHODS =====

        private async Task<BookingListItemDto> MapToBookingListItemDto(Booking booking)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            var bookingRooms = (await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId)).ToList();
            
            var roomNumbers = new List<string>();
            var roomTypes = new List<string>();

            foreach (var br in bookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(br.RoomId);
                if (room != null)
                {
                    roomNumbers.Add(room.RoomName);
                    var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                    if (roomType != null && !roomTypes.Contains(roomType.TypeName))
                        roomTypes.Add(roomType.TypeName);
                }
            }

            var bookingStatus = booking.PaymentStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId.Value))?.CodeValue : "N/A";
            var paymentStatus = booking.PaymentStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId.Value))?.CodeValue : "N/A";
            var depositStatus = booking.DepositStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.DepositStatusId.Value))?.CodeValue : "N/A";
            var bookingType = booking.BookingTypeId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId.Value))?.CodeValue : "N/A";

            var transactions = (await _unitOfWork.Transactions.FindAsync(t => t.BookingId == bookingId)).ToList();
            var paidAmount = transactions.Sum(t => t.TotalAmount);

            var createdByEmployee = booking.CreatedBy.HasValue ?
                (await _unitOfWork.Employees.GetByIdAsync(booking.CreatedBy.Value))?.FullName : null;

            return new BookingListItemDto
            {
                BookingId = booking.BookingId,
                BookingCode = $"BK{booking.BookingId:D6}",
                CustomerId = booking.CustomerId,
                CustomerName = customer?.FullName ?? "N/A",
                CustomerPhone = customer?.PhoneNumber ?? "N/A",
                CustomerEmail = customer?.Account?.Email ?? "N/A",
                BookingType = bookingType ?? "N/A",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalNights = (booking.CheckOutDate - booking.CheckInDate).Days,
                TotalRooms = bookingRooms.Count,
                RoomNumbers = roomNumbers,
                RoomTypes = roomTypes,
                TotalAmount = booking.TotalAmount,
                DepositAmount = booking.DepositAmount,
                PaidAmount = paidAmount,
                RemainingAmount = booking.TotalAmount - paidAmount,
                BookingStatus = bookingStatus ?? "N/A",
                PaymentStatus = paymentStatus ?? "N/A",
                DepositStatus = depositStatus ?? "N/A",
                CreatedAt = booking.CreatedAt,
                CreatedByEmployee = createdByEmployee,
                UpdatedAt = booking.UpdatedAt
            };
        }

        private async Task<BookingDetailDto> MapToBookingDetailDto(Booking booking)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            var bookingRooms = (await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId)).ToList();
            var transactions = (await _unitOfWork.Transactions.FindAsync(t => t.BookingId == booking.BookingId)).ToList();

            // Map customer
            var customerBookings = (await _unitOfWork.Bookings.FindAsync(b => b.CustomerId == booking.CustomerId)).ToList();
            var customerDetail = new CustomerDetailDto
            {
                CustomerId = customer!.CustomerId,
                FullName = customer.FullName,
                Email = customer.Account?.Email ?? "",
                PhoneNumber = customer.PhoneNumber ?? "",
                IdentityCard = customer.IdentityCard,
                Address = customer.Address,
                DateOfBirth = customer.DateOfBirth,
                Gender = customer.Gender,
                TotalBookings = customerBookings.Count,
                LastBookingDate = customerBookings.OrderByDescending(b => b.CreatedAt).FirstOrDefault()?.CreatedAt,
                TotalSpent = customerBookings.Sum(b => b.TotalAmount)
            };

            // Map rooms
            var rooms = new List<BookingRoomDetailDto>();
            foreach (var br in bookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(br.RoomId);
                if (room == null) continue;

                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                var roomImages = (await _unitOfWork.RoomImages.FindAsync(ri => ri.RoomId == room.RoomId))
                    .Select(ri => ri.FilePath).ToList();
                var amenities = (await _unitOfWork.RoomAmenities.FindAsync(ra => ra.RoomId == room.RoomId))
                    .Select(ra => ra.Amenity?.AmenityName ?? "").Where(a => !string.IsNullOrEmpty(a)).ToList();

                rooms.Add(new BookingRoomDetailDto
                {
                    BookingRoomId = br.BookingRoomId,
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomName,
                    RoomTypeName = roomType?.TypeName ?? "",
                    RoomTypeCode = roomType?.CodeValue ?? "",
                    PricePerNight = br.PricePerNight,
                    NumberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days,
                    SubTotal = br.PricePerNight * (booking.CheckOutDate - booking.CheckInDate).Days,
                    Status = "Active",
                    RoomImages = roomImages,
                    MaxOccupancy = roomType?.MaxOccupancy ?? 0,
                    RoomSize = roomType?.RoomSize ?? 0,
                    BedType = roomType?.BedType ?? "",
                    Amenities = amenities
                });
            }

            // Map payment history
            var paymentHistory = new List<PaymentHistoryDetailDto>();
            foreach (var trans in transactions)
            {
                var paymentMethod = trans.PaymentMethodId.HasValue ?
                    await _unitOfWork.CommonCodes.GetByIdAsync(trans.PaymentMethodId.Value) : null;
                var transStatus = trans.TransactionStatusId.HasValue ?
                    await _unitOfWork.CommonCodes.GetByIdAsync(trans.TransactionStatusId.Value) : null;
                var employee = trans.CreatedBy.HasValue ?
                    await _unitOfWork.Employees.GetByIdAsync(trans.CreatedBy.Value) : null;

                paymentHistory.Add(new PaymentHistoryDetailDto
                {
                    TransactionId = trans.TransactionId,
                    TransactionCode = $"TXN{trans.TransactionId:D6}",
                    Amount = trans.TotalAmount,
                    PaymentMethod = paymentMethod?.CodeValue ?? "N/A",
                    TransactionType = trans.DepositAmount.HasValue ? "Deposit" : "FullPayment",
                    Status = transStatus?.CodeValue ?? "N/A",
                    Note = trans.TransactionRef,
                    TransactionReference = trans.TransactionRef,
                    ProcessedAt = trans.CreatedAt,
                    ProcessedBy = employee?.FullName ?? "System"
                });
            }

            var bookingStatus = booking.PaymentStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId.Value))?.CodeValue : "N/A";
            var paymentStatus = booking.PaymentStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId.Value))?.CodeValue : "N/A";
            var depositStatus = booking.DepositStatusId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.DepositStatusId.Value))?.CodeValue : "N/A";
            var bookingType = booking.BookingTypeId.HasValue ?
                (await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId.Value))?.CodeValue : "N/A";

            var createdByEmployee = booking.CreatedBy.HasValue ?
                (await _unitOfWork.Employees.GetByIdAsync(booking.CreatedBy.Value))?.FullName : null;
            var updatedByEmployee = booking.UpdatedBy.HasValue ?
                (await _unitOfWork.Employees.GetByIdAsync(booking.UpdatedBy.Value))?.FullName : null;

            var paidAmount = transactions.Sum(t => t.TotalAmount);

            return new BookingDetailDto
            {
                BookingId = booking.BookingId,
                BookingCode = $"BK{booking.BookingId:D6}",
                BookingType = bookingType ?? "N/A",
                Customer = customerDetail,
                Rooms = rooms,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalNights = (booking.CheckOutDate - booking.CheckInDate).Days,
                SubTotal = booking.TotalAmount,
                TaxAmount = 0,
                ServiceCharge = 0,
                TotalAmount = booking.TotalAmount,
                DepositAmount = booking.DepositAmount,
                PaidAmount = paidAmount,
                RemainingAmount = booking.TotalAmount - paidAmount,
                BookingStatus = bookingStatus ?? "N/A",
                PaymentStatus = paymentStatus ?? "N/A",
                DepositStatus = depositStatus ?? "N/A",
                SpecialRequests = booking.SpecialRequests,
                PaymentHistory = paymentHistory,
                BookingHistory = new List<BookingHistoryDto>(),
                CreatedAt = booking.CreatedAt,
                CreatedByEmployee = createdByEmployee,
                UpdatedAt = booking.UpdatedAt,
                UpdatedByEmployee = updatedByEmployee
            };
        }
    }
}
