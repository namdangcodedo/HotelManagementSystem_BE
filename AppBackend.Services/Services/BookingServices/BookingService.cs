using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Helpers;
using AppBackend.Services.BackgroundJobs;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AppBackend.Services.ApiModels.TransactionModel;

namespace AppBackend.Services.Services.BookingServices;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CacheHelper _cacheHelper;
    private readonly BookingHelperService _bookingHelper;
    private readonly BookingTokenHelper _tokenHelper;
    private readonly QRPaymentHelper _qrPaymentHelper;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly BookingTimeoutChecker _timeoutChecker;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        CacheHelper cacheHelper,
        BookingHelperService bookingHelper,
        BookingTokenHelper tokenHelper,
        QRPaymentHelper qrPaymentHelper,
        IEmailService emailService,
        IConfiguration configuration,
        BookingTimeoutChecker timeoutChecker,
        ILogger<BookingService> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheHelper = cacheHelper;
        _bookingHelper = bookingHelper;
        _tokenHelper = tokenHelper;
        _qrPaymentHelper = qrPaymentHelper;
        _emailService = emailService;
        _configuration = configuration;
        _timeoutChecker = timeoutChecker;
        _logger = logger;
    }

    /// <summary>
    /// Kiểm tra phòng có sẵn để đặt không
    /// </summary>
    public async Task<ResultModel> CheckRoomAvailabilityAsync(CheckRoomAvailabilityRequest request)
    {
        try
        {
            var response = new CheckAvailabilityResponse
            {
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalNights = _bookingHelper.CalculateNumberOfNights(request.CheckInDate, request.CheckOutDate),
                RoomTypes = new List<RoomTypeAvailabilityDto>()
            };

            bool allAvailable = true;

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeRequest.RoomTypeId);
                if (roomType == null)
                {
                    allAvailable = false;
                    response.RoomTypes.Add(new RoomTypeAvailabilityDto
                    {
                        RoomTypeId = roomTypeRequest.RoomTypeId,
                        RequestedQuantity = roomTypeRequest.Quantity,
                        AvailableCount = 0,
                        IsAvailable = false,
                        Message = "Loại phòng không tồn tại"
                    });
                    continue;
                }

                // Tìm phòng available
                var availableRooms = await _bookingHelper.FindAvailableRoomsByTypeAsync(
                    roomTypeRequest.RoomTypeId,
                    roomTypeRequest.Quantity,
                    request.CheckInDate,
                    request.CheckOutDate
                );

                bool isAvailable = availableRooms.Count >= roomTypeRequest.Quantity;
                if (!isAvailable) allAvailable = false;

                response.RoomTypes.Add(new RoomTypeAvailabilityDto
                {
                    RoomTypeId = roomType.RoomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    Description = roomType.Description ?? "",
                    BasePriceNight = roomType.BasePriceNight,
                    MaxOccupancy = roomType.MaxOccupancy,
                    RoomSize = roomType.RoomSize ?? 0,
                    NumberOfBeds = roomType.NumberOfBeds ?? 0,
                    BedType = roomType.BedType ?? "",
                    AvailableCount = availableRooms.Count,
                    RequestedQuantity = roomTypeRequest.Quantity,
                    IsAvailable = isAvailable,
                    Message = isAvailable
                        ? $"Có {availableRooms.Count} phòng trống"
                        : $"Chỉ còn {availableRooms.Count}/{roomTypeRequest.Quantity} phòng trống",
                    Images = new List<string>() // TODO: Load images if needed
                });
            }

            response.IsAllAvailable = allAvailable;
            response.Message = allAvailable
                ? "Tất cả phòng đều có sẵn"
                : "Một số loại phòng không đủ số lượng";

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = allAvailable ? StatusCodes.Status200OK : StatusCodes.Status409Conflict,
                Message = response.Message,
                Data = response
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
    /// Tạo booking cho customer đã có tài khoản
    /// </summary>
    public async Task<ResultModel> CreateBookingAsync(CreateBookingRequest request, int userId)
    {
        try
        {
            // 1. Validate
            if (request.CheckInDate <= DateTime.UtcNow)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Ngày check-in phải sau thời điểm hiện tại"
                };
            }

            if (request.CheckOutDate <= request.CheckInDate)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Ngày check-out phải sau ngày check-in"
                };
            }

            // 2. Lấy customer từ userId
            var account = await _unitOfWork.Accounts.GetByIdAsync(userId);
            if (account == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy tài khoản"
                };
            }

            var customer = (await _unitOfWork.Customers.FindAsync(c => c.AccountId == userId)).FirstOrDefault();
            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy thông tin khách hàng"
                };
            }

            // 3. Tìm phòng available
            var selectedRooms = new List<Room>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();
            decimal totalAmount = 0;

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeRequest.RoomTypeId);
                if (roomType == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Không tìm thấy loại phòng ID: {roomTypeRequest.RoomTypeId}"
                    };
                }

                // Tìm phòng available
                var availableRooms = await _bookingHelper.FindAvailableRoomsByTypeAsync(
                    roomTypeRequest.RoomTypeId,
                    roomTypeRequest.Quantity,
                    request.CheckInDate,
                    request.CheckOutDate
                );

                if (availableRooms.Count < roomTypeRequest.Quantity)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = $"Không đủ phòng {roomType.TypeName}. Còn {availableRooms.Count}/{roomTypeRequest.Quantity}"
                    };
                }

                // Chọn phòng
                selectedRooms.AddRange(availableRooms);

                // Tính giá
                decimal roomTypeTotal = 0;
                foreach (var room in availableRooms)
                {
                    var roomPrice = await _bookingHelper.CalculateRoomPriceWithHolidayAsync(
                        room.RoomId,
                        request.CheckInDate,
                        request.CheckOutDate,
                        roomType.BasePriceNight
                    );
                    roomTypeTotal += roomPrice;
                }

                totalAmount += roomTypeTotal;

                roomTypeDetails.Add(new RoomTypeQuantityDto
                {
                    RoomTypeId = roomType.RoomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    Quantity = roomTypeRequest.Quantity,
                    PricePerNight = roomType.BasePriceNight,
                    SubTotal = roomTypeTotal
                });
            }

            // 4. Tính deposit (30%)
            decimal depositAmount = totalAmount * 0.3m;

            // 5. Lấy status codes
            var pendingStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Pending")).FirstOrDefault();
            var onlineType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == "Online")).FirstOrDefault();

            if (pendingStatus == null || onlineType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Lỗi cấu hình hệ thống: Thiếu status codes"
                };
            }

            // 6. Tạo booking trong database
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                StatusId = pendingStatus.CodeId,
                BookingTypeId = onlineType.CodeId,
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // 7. Tạo BookingRooms
            var numberOfNights = _bookingHelper.CalculateNumberOfNights(request.CheckInDate, request.CheckOutDate);
            foreach (var room in selectedRooms)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                var pricePerNight = roomType?.BasePriceNight ?? 0;
                
                var bookingRoom = new BookingRoom
                {
                    BookingId = booking.BookingId,
                    RoomId = room.RoomId,
                    PricePerNight = pricePerNight,
                    NumberOfNights = numberOfNights,
                    SubTotal = pricePerNight * numberOfNights,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate
                };
                await _unitOfWork.BookingRooms.AddAsync(bookingRoom);
            }
            await _unitOfWork.SaveChangesAsync();

            // 8. Tạo QR Code để thanh toán
            var transactionRef = _qrPaymentHelper.GenerateTransactionRef(booking.BookingId);
            var description = $"Dat coc booking {booking.BookingId}";
            var qrPaymentInfo = await _qrPaymentHelper.GenerateQRPaymentInfoAsync(
                _unitOfWork,
                _configuration,
                depositAmount,
                description,
                transactionRef
            );

            // 9. Lưu thông tin vào cache để tracking
            _cacheHelper.Set(CachePrefix.BookingPayment, booking.BookingId.ToString(), new
            {
                booking.BookingId,
                TransactionRef = transactionRef,
                RoomIds = selectedRooms.Select(r => r.RoomId).ToList(),
                request.CheckInDate,
                request.CheckOutDate,
                Amount = depositAmount
            }, TimeSpan.FromMinutes(15));

            // 10. Schedule timeout check - Tự động cancel nếu không thanh toán sau 15 phút
            _timeoutChecker.ScheduleTimeoutCheck(booking.BookingId, delayMinutes: 15);

            // 11. Generate booking token
            var bookingToken = _tokenHelper.EncodeBookingId(booking.BookingId);

            // 12. Return response với QR payment info
            var response = new BookingDto
            {
                BookingId = booking.BookingId,
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                RoomIds = selectedRooms.Select(r => r.RoomId).ToList(),
                RoomNames = selectedRooms.Select(r => r.RoomName).ToList(),
                RoomTypeDetails = roomTypeDetails,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                PaymentStatus = "Pending",
                BookingType = "Online",
                SpecialRequests = request.SpecialRequests,
                CreatedAt = booking.CreatedAt,
                OrderCode = transactionRef
            };

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo booking thành công. Vui lòng quét mã QR để thanh toán trong 15 phút!",
                Data = new
                {
                    Booking = response,
                    BookingToken = bookingToken,
                    QRPayment = qrPaymentInfo,
                    PaymentDeadline = DateTime.UtcNow.AddMinutes(15)
                }
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
    /// Tạo booking cho khách vãng lai (không cần tài khoản)
    /// </summary>
    public async Task<ResultModel> CreateGuestBookingAsync(CreateGuestBookingRequest request)
    {
        try
        {
            // 1. Validate request
            if (request == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Request không hợp lệ"
                };
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Họ tên không được để trống"
                };
            }

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Số điện thoại không được để trống"
                };
            }

            if (request.RoomTypes == null || request.RoomTypes.Count == 0)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Phải chọn ít nhất một loại phòng"
                };
            }

            if (request.CheckInDate <= DateTime.UtcNow)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Ngày check-in phải sau thời điểm hiện tại"
                };
            }

            if (request.CheckOutDate <= request.CheckInDate)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Ngày check-out phải sau ngày check-in"
                };
            }

            // 2. Tìm hoặc tạo customer
            // Chỉ tìm kiếm theo email nếu email không rỗng
            var existingCustomer = string.IsNullOrWhiteSpace(request.Email)
                ? (await _unitOfWork.Customers.FindAsync(c =>
                    c.PhoneNumber == request.PhoneNumber
                )).FirstOrDefault()
                : (await _unitOfWork.Customers.FindAsync(c =>
                    c.PhoneNumber == request.PhoneNumber ||
                    (c.Account != null && c.Account.Email == request.Email)
                )).FirstOrDefault();

            Customer customer;
            if (existingCustomer != null)
            {
                customer = existingCustomer;
                // Cập nhật thông tin customer nếu khác
                if (!string.IsNullOrEmpty(request.FullName) && customer.FullName != request.FullName)
                    customer.FullName = request.FullName;
                if (!string.IsNullOrEmpty(request.IdentityCard) && customer.IdentityCard != request.IdentityCard)
                    customer.IdentityCard = request.IdentityCard;
                if (!string.IsNullOrEmpty(request.Address) && customer.Address != request.Address)
                    customer.Address = request.Address;
                await _unitOfWork.Customers.UpdateAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Tạo customer mới - Nếu có email thì tạo luôn tài khoản
                Account? newAccount = null;
                
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    // Kiểm tra email đã tồn tại chưa
                    var existingAccount = (await _unitOfWork.Accounts.FindAsync(a => a.Email == request.Email)).FirstOrDefault();
                    
                    if (existingAccount == null)
                    {
                        // Tạo tài khoản mới cho guest với password random
                        var randomPassword = Guid.NewGuid().ToString("N").Substring(0, 12) + "Aa@1"; // Password phức tạp
                        newAccount = new Account
                        {
                            Username = request.Email.Split('@')[0] + "_" + DateTime.UtcNow.Ticks.ToString().Substring(0, 6),
                            Email = request.Email,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword, 12),
                            IsLocked = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Accounts.AddAsync(newAccount);
                        await _unitOfWork.SaveChangesAsync();
                        
                        // Gán role User cho guest account
                        var userRole = (await _unitOfWork.Roles.FindAsync(r => r.RoleValue == "User")).FirstOrDefault();
                        if (userRole != null)
                        {
                            var accountRole = new AccountRole
                            {
                                AccountId = newAccount.AccountId,
                                RoleId = userRole.RoleId
                            };
                            await _unitOfWork.Roles.AddAccountRoleAsync(newAccount.AccountId, userRole.RoleId);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        
                        _logger.LogInformation("[CreateGuestBooking] Created new account for guest with email: {Email}", request.Email);
                    }
                    else
                    {
                        newAccount = existingAccount;
                        _logger.LogInformation("[CreateGuestBooking] Found existing account for email: {Email}", request.Email);
                    }
                }
                
                customer = new Customer
                {
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    IdentityCard = request.IdentityCard,
                    Address = request.Address,
                    AccountId = newAccount?.AccountId, // Liên kết với account nếu có
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
                
                // Gửi email welcome cho guest nếu có email
                if (newAccount != null && !string.IsNullOrWhiteSpace(request.Email))
                {
                    try
                    {
                        // TODO: Implement welcome email with login instructions
                        _logger.LogInformation("[CreateGuestBooking] Welcome email should be sent to: {Email}", request.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "[CreateGuestBooking] Failed to send welcome email");
                    }
                }
            }

            // 3. Tìm phòng available
            var selectedRooms = new List<Room>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();
            decimal totalAmount = 0;

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeRequest.RoomTypeId);
                if (roomType == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Không tìm thấy loại phòng ID: {roomTypeRequest.RoomTypeId}"
                    };
                }

                var availableRooms = await _bookingHelper.FindAvailableRoomsByTypeAsync(
                    roomTypeRequest.RoomTypeId,
                    roomTypeRequest.Quantity,
                    request.CheckInDate,
                    request.CheckOutDate
                );

                if (availableRooms.Count < roomTypeRequest.Quantity)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = $"Không đủ phòng {roomType.TypeName}"
                    };
                }

                // Chọn phòng
                selectedRooms.AddRange(availableRooms);

                // Tính giá
                decimal roomTypeTotal = 0;
                foreach (var room in availableRooms)
                {
                    var roomPrice = await _bookingHelper.CalculateRoomPriceWithHolidayAsync(
                        room.RoomId,
                        request.CheckInDate,
                        request.CheckOutDate,
                        roomType.BasePriceNight
                    );
                    roomTypeTotal += roomPrice;
                }

                totalAmount += roomTypeTotal;

                roomTypeDetails.Add(new RoomTypeQuantityDto
                {
                    RoomTypeId = roomType.RoomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    Quantity = roomTypeRequest.Quantity,
                    PricePerNight = roomType.BasePriceNight,
                    SubTotal = roomTypeTotal
                });
            }

            // 4. Tính deposit (30%)
            decimal depositAmount = totalAmount * 0.3m;

            // 5. Lấy status codes
            var pendingStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Pending")).FirstOrDefault();
            var onlineType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == "Online")).FirstOrDefault();

            if (pendingStatus == null || onlineType == null)
            {
                _logger.LogError("[CreateGuestBooking] Missing status codes: Pending={0}, Online={1}", 
                    pendingStatus == null ? "NULL" : pendingStatus.CodeId.ToString(),
                    onlineType == null ? "NULL" : onlineType.CodeId.ToString());
                
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Lỗi cấu hình hệ thống: Thiếu status codes (Pending hoặc Online)"
                };
            }

            // 6. Tạo booking trong database
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                StatusId = pendingStatus.CodeId,
                BookingTypeId = onlineType.CodeId,
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            var numberOfNights2 = _bookingHelper.CalculateNumberOfNights(request.CheckInDate, request.CheckOutDate);
            foreach (var room in selectedRooms)
            {
                if (room == null)
                {
                    _logger.LogWarning("[CreateGuestBooking] Encountered null room in selectedRooms");
                    continue;
                }

                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                if (roomType == null)
                {
                    _logger.LogWarning("[CreateGuestBooking] Room {0} has invalid RoomTypeId {1}", room.RoomId, room.RoomTypeId);
                    continue;
                }

                var pricePerNight = roomType.BasePriceNight;
                
                var bookingRoom = new BookingRoom
                {
                    BookingId = booking.BookingId,
                    RoomId = room.RoomId,
                    PricePerNight = pricePerNight,
                    NumberOfNights = numberOfNights2,
                    SubTotal = pricePerNight * numberOfNights2,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate
                };
                await _unitOfWork.BookingRooms.AddAsync(bookingRoom);
            }
            await _unitOfWork.SaveChangesAsync();

            // 8. Tạo QR Code để thanh toán
            var transactionRef = _qrPaymentHelper.GenerateTransactionRef(booking.BookingId);
            var description = $"Dat coc booking {booking.BookingId}";
            
            QRPaymentInfoDto? qrPaymentInfo = null;
            try
            {
                qrPaymentInfo = await _qrPaymentHelper.GenerateQRPaymentInfoAsync(
                    _unitOfWork,
                    _configuration,
                    depositAmount,
                    description,
                    transactionRef
                );
                
                if (qrPaymentInfo == null)
                {
                    _logger.LogWarning("[CreateGuestBooking] QR Payment generation returned null, continuing without QR info");
                }
            }
            catch (Exception qrEx)
            {
                _logger.LogError(qrEx, "[CreateGuestBooking] Exception in QR generation");
                // Continue even if QR generation fails - booking is still valid
            }

            // 9. Lưu thông tin vào cache để tracking
            _cacheHelper.Set(CachePrefix.BookingPayment, booking.BookingId.ToString(), new
            {
                booking.BookingId,
                TransactionRef = transactionRef,
                RoomIds = selectedRooms.Select(r => r.RoomId).ToList(),
                request.CheckInDate,
                request.CheckOutDate,
                Amount = depositAmount
            }, TimeSpan.FromMinutes(15));

            // 10. Schedule timeout check - Tự động cancel nếu không thanh toán sau 15 phút
            _timeoutChecker.ScheduleTimeoutCheck(booking.BookingId, delayMinutes: 15);

            // 11. Generate booking token
            var bookingToken = _tokenHelper.EncodeBookingId(booking.BookingId);

            // 12. Return response với QR payment info
            var response = new BookingDto
            {
                BookingId = booking.BookingId,
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                RoomIds = selectedRooms.Select(r => r.RoomId).ToList(),
                RoomNames = selectedRooms.Select(r => r.RoomName).ToList(),
                RoomTypeDetails = roomTypeDetails,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                PaymentStatus = "Pending",
                BookingType = "Online",
                SpecialRequests = request.SpecialRequests,
                CreatedAt = booking.CreatedAt,
                OrderCode = transactionRef
            };

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo booking thành công. Vui lòng quét mã QR để thanh toán trong 15 phút!",
                Data = new
                {
                    Booking = response,
                    BookingToken = bookingToken,
                    QRPayment = qrPaymentInfo,
                    PaymentDeadline = DateTime.UtcNow.AddMinutes(15)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateGuestBooking] Unexpected error: {0}", ex.Message);
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Lỗi: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Lấy thông tin booking theo ID
    /// </summary>
    public async Task<ResultModel> GetBookingByIdAsync(int bookingId)
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

            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            var rooms = new List<Room>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();

            foreach (var br in bookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(br.RoomId);
                if (room != null)
                {
                    rooms.Add(room);
                }
            }

            // Group by room type
            var roomsByType = rooms.GroupBy(r => r.RoomTypeId);
            foreach (var group in roomsByType)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(group.Key);
                if (roomType != null)
                {
                    roomTypeDetails.Add(new RoomTypeQuantityDto
                    {
                        RoomTypeId = roomType.RoomTypeId,
                        RoomTypeName = roomType.TypeName,
                        RoomTypeCode = roomType.TypeCode,
                        Quantity = group.Count(),
                        PricePerNight = roomType.BasePriceNight
                    });
                }
            }

            var statusCode = booking.StatusId.HasValue
                ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                : null;
            var bookingTypeCode = booking.BookingTypeId.HasValue
                ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId.Value)
                : null;

            var response = new BookingDto
            {
                BookingId = booking.BookingId,
                CustomerId = booking.CustomerId,
                CustomerName = customer?.FullName ?? "",
                RoomIds = rooms.Select(r => r.RoomId).ToList(),
                RoomNames = rooms.Select(r => r.RoomName).ToList(),
                RoomTypeDetails = roomTypeDetails,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                TotalAmount = booking.TotalAmount,
                DepositAmount = booking.DepositAmount,
                PaymentStatus = statusCode?.CodeValue ?? "Unknown",
                BookingType = bookingTypeCode?.CodeValue ?? "Unknown",
                SpecialRequests = booking.SpecialRequests,
                CreatedAt = booking.CreatedAt
            };

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = response
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
    /// Lấy booking theo token (dành cho guest)
    /// </summary>
    public async Task<ResultModel> GetBookingByTokenAsync(string token)
    {
        try
        {
            var bookingId = _tokenHelper.DecodeBookingToken(token);
            return await GetBookingByIdAsync(bookingId);
        }
        catch (Exception ex)
        {
            return new ResultModel
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message = $"Token không hợp lệ: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Xử lý thanh toán (webhook từ PayOS hoặc manual confirm)
    /// Chuyển từ Pending → DepositPaid hoặc hủy booking
    /// </summary>
    public async Task<ResultModel> ProcessPaymentAsync(ConfirmPaymentRequest request)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
            if (booking == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy booking"
                };
            }

            var cacheKey = request.BookingId.ToString();

            if (request.IsCancel)
            {
                // Hủy booking
                var cancelledStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && c.CodeName == "Cancelled")).FirstOrDefault();

                if (cancelledStatus != null)
                {
                    booking.StatusId = cancelledStatus.CodeId;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.UpdateAsync(booking);
                    await _unitOfWork.SaveChangesAsync();
                }

                _cacheHelper.Remove(CachePrefix.BookingPayment, cacheKey);

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Đã hủy booking thành công."
                };
            }

            // Khách thông báo đã chuyển khoản → Chuyển sang PendingConfirmation
            var pendingConfirmationStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "PendingConfirmation")).FirstOrDefault();

            if (pendingConfirmationStatus == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Lỗi: Không tìm thấy status PendingConfirmation"
                };
            }

            booking.StatusId = pendingConfirmationStatus.CodeId;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email cho manager để kiểm tra bill ngân hàng
            try
            {
                await _emailService.SendPaymentConfirmationRequestEmailToManagerAsync(request.BookingId);
                _logger.LogInformation("[ProcessPayment] Payment confirmation request sent to manager for booking {BookingId}", request.BookingId);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "[ProcessPayment] Failed to send email to manager for booking {BookingId}", request.BookingId);
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cảm ơn bạn đã thông báo thanh toán. Quản lý sẽ kiểm tra bill ngân hàng và xác nhận trong thời gian sớm nhất."
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
    /// Manager kiểm tra bill ngân hàng và xác nhận thanh toán
    /// Chuyển status từ PendingConfirmation → Confirmed
    /// </summary>
    public async Task<ResultModel> ConfirmOnlineBookingAsync(int bookingId)
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

            // Lấy thông tin phòng đã đặt
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            if (bookingRooms == null || !bookingRooms.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy thông tin phòng đã đặt"
                };
            }

            // Tính tổng số tiền và deposit từ thông tin phòng
            decimal totalAmount = 0;
            foreach (var br in bookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetSingleAsync(
                    r => r.RoomId == br.RoomId,
                    r => r.RoomType);
                if (room != null)
                {
                    totalAmount += room.RoomType.BasePriceNight * br.NumberOfNights;
                }
            }
            decimal depositAmount = totalAmount * 0.3m;

            // Cập nhật thông tin booking
            booking.TotalAmount = totalAmount;
            booking.DepositAmount = depositAmount;

            // Chuyển trạng thái booking từ PendingConfirmation → Confirmed
            var confirmedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Confirmed")).FirstOrDefault();

            if (confirmedStatus == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Lỗi: Không tìm thấy status Confirmed"
                };
            }

            booking.StatusId = confirmedStatus.CodeId;
            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.UpdateAsync(booking);

            // Tạo transaction record
            var completedPaymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentStatus" && c.CodeName == "Completed")).FirstOrDefault();
            var paymentMethodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentMethod" && c.CodeName == "Bank")).FirstOrDefault();

            var newTransaction = new AppBackend.BusinessObjects.Models.Transaction
            {
                BookingId = booking.BookingId,
                TotalAmount = booking.DepositAmount,
                PaidAmount = booking.DepositAmount,
                PaymentMethodId = paymentMethodCode?.CodeId ?? 0,
                PaymentStatusId = completedPaymentStatus?.CodeId ?? 0,
                OrderCode = "", // Không cần OrderCode ở đây
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Transactions.AddAsync(newTransaction);
            await _unitOfWork.SaveChangesAsync();

            // Gửi email thông báo xác nhận booking đến khách hàng
            try
            {
                await _emailService.SendBookingConfirmationEmailAsync(booking.BookingId);
                _logger.LogInformation("[ConfirmOnlineBooking] Booking confirmation email sent to customer for booking {BookingId}", booking.BookingId);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "[ConfirmOnlineBooking] Failed to send booking confirmation email for booking {BookingId}", booking.BookingId);
                // Không throw exception - chỉ ghi log lỗi gửi email
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xác nhận thanh toán thành công. Booking đã được xác nhận."
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
    /// Lấy danh sách booking của user
    /// </summary>
    public async Task<ResultModel> GetMyBookingsByAccountIdAsync(int accountId)
    {
        try
        {
            var customer = (await _unitOfWork.Customers.FindAsync(c => c.AccountId == accountId)).FirstOrDefault();
            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Không tìm thấy thông tin khách hàng"
                };
            }

            var bookings = await _unitOfWork.Bookings.FindAsync(b => b.CustomerId == customer.CustomerId);
            var bookingDtos = new List<BookingDto>();

            foreach (var booking in bookings.OrderByDescending(b => b.CreatedAt))
            {
                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId);
                var rooms = new List<Room>();

                foreach (var br in bookingRooms)
                {
                    var room = await _unitOfWork.Rooms.GetByIdAsync(br.RoomId);
                    if (room != null) rooms.Add(room);
                }

                var statusCode = booking.StatusId.HasValue
                    ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                    : null;
                var bookingTypeCode = booking.BookingTypeId.HasValue
                    ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId.Value)
                    : null;

                bookingDtos.Add(new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = booking.CustomerId,
                    CustomerName = customer.FullName,
                    RoomIds = rooms.Select(r => r.RoomId).ToList(),
                    RoomNames = rooms.Select(r => r.RoomName).ToList(),
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentStatus = statusCode?.CodeValue ?? "Unknown",
                    BookingType = bookingTypeCode?.CodeValue ?? "Unknown",
                    SpecialRequests = booking.SpecialRequests,
                    CreatedAt = booking.CreatedAt
                });
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = bookingDtos,
                Message = $"Tìm thấy {bookingDtos.Count} booking"
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
    /// Lấy thông tin QR payment theo BookingId
    /// Nhân viên chỉ cần BookingId để lấy QR code hiển thị cho khách
    /// </summary>
    public async Task<ResultModel> GetQRPaymentInfoAsync(int bookingId)
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

            // Only allow QR payment for Pending bookings
            var statusCode = booking.StatusId.HasValue
                ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                : null;

            if (statusCode == null || statusCode.CodeName != "Pending")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ hỗ trợ QR payment cho booking đang ở trạng thái Pending"
                };
            }

            var transactionRef = _qrPaymentHelper.GenerateTransactionRef(booking.BookingId);
            var description = $"Dat coc booking {booking.BookingId}";
            var qrPaymentInfo = await _qrPaymentHelper.GenerateQRPaymentInfoAsync(
                _unitOfWork,
                _configuration,
                booking.DepositAmount,
                description,
                transactionRef
            );

            if (qrPaymentInfo == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Không thể tạo QR payment do lỗi cấu hình ngân hàng"
                };
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin QR payment thành công",
                Data = new
                {
                    BookingId = bookingId,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    QRPayment = qrPaymentInfo,
                    PaymentDeadline = booking.CreatedAt.AddMinutes(15)
                }
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
}
