using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Helpers;
using AppBackend.Services.Services.Email;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.BookingServices;

public class BookingManagementService : IBookingManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly CacheHelper _cacheHelper;
    private readonly BookingHelperService _bookingHelper;
    private readonly QRPaymentHelper _qrPaymentHelper;
    private readonly IEmailService _emailService;

    public BookingManagementService(
        IUnitOfWork unitOfWork,
        CacheHelper cacheHelper,
        BookingHelperService bookingHelper,
        QRPaymentHelper qrPaymentHelper,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _cacheHelper = cacheHelper;
        _bookingHelper = bookingHelper;
        _qrPaymentHelper = qrPaymentHelper;
        _emailService = emailService;
    }

    public async Task<ResultModel> CreateOfflineBookingAsync(CreateOfflineBookingRequest request, int employeeId)
    {
        try
        {
            // 1. Validate dates
            if (request.CheckOutDate <= request.CheckInDate)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Ngày check-out phải sau ngày check-in"
                };
            }

            // 2. Validate RoomIds
            if (request.RoomIds == null || !request.RoomIds.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Vui lòng chọn ít nhất một phòng"
                };
            }

            // 3. Tìm hoặc tạo customer
            var existingCustomer = (await _unitOfWork.Customers.FindAsync(c =>
                c.PhoneNumber == request.PhoneNumber ||
                (c.Account != null && c.Account.Email == request.Email)
            )).FirstOrDefault();

            Customer customer;
            if (existingCustomer != null)
            {
                customer = existingCustomer;
            }
            else
            {
                customer = new Customer
                {
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    IdentityCard = request.IdentityCard,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }

            // 4. Validate các phòng được chọn
            var selectedRooms = new List<Room>();
            foreach (var roomId in request.RoomIds)
            {
                var room = await _unitOfWork.Rooms.GetByIdAsync(roomId);
                if (room == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = $"Không tìm thấy phòng ID: {roomId}"
                    };
                }

                // Kiểm tra phòng có available không
                var isAvailable = await _bookingHelper.IsRoomAvailableAsync(
                    roomId,
                    request.CheckInDate,
                    request.CheckOutDate
                );

                if (!isAvailable)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = $"Phòng {room.RoomName} không còn trống trong thời gian này"
                    };
                }

                selectedRooms.Add(room);
            }

            // 5. Tính toán giá tiền cho từng phòng và tổng tiền
            var roomTypeDetails = new List<RoomTypeQuantityDto>();
            var roomTypeGroups = selectedRooms.GroupBy(r => r.RoomTypeId);
            decimal totalAmount = 0;

            foreach (var group in roomTypeGroups)
            {
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(group.Key);
                if (roomType == null) continue;

                decimal roomTypeTotal = 0;
                foreach (var room in group)
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
                    Quantity = group.Count(),
                    PricePerNight = roomType.BasePriceNight,
                    SubTotal = roomTypeTotal
                });
            }

            // 6. Tính deposit (30%)
            decimal depositAmount = totalAmount * 0.3m;

            // 7. Lấy status codes - Booking offline mặc định là CheckedIn
            var checkedInStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "CheckedIn")).FirstOrDefault();
            var walkInType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == "WalkIn")).FirstOrDefault();

            if (checkedInStatus == null || walkInType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Lỗi cấu hình hệ thống: Thiếu status codes"
                };
            }

            // 8. Tạo booking trong database
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                StatusId = checkedInStatus.CodeId,
                BookingTypeId = walkInType.CodeId,
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = employeeId
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // 9. Tạo BookingRooms
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

            // 10. Tạo transaction nếu đã thanh toán ngay
            var paymentMethodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentMethod" && c.CodeName == request.PaymentMethod)).FirstOrDefault();
            var completedPaymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentStatus" && c.CodeName == "Completed")).FirstOrDefault();

            var transaction = new Transaction
            {
                BookingId = booking.BookingId,
                TotalAmount = totalAmount,
                PaidAmount = totalAmount,
                PaymentMethodId = paymentMethodCode?.CodeId ?? 0,
                PaymentStatusId = completedPaymentStatus?.CodeId ?? 0,
                TransactionRef = $"WALKIN-{booking.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = employeeId
            };
            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            // 11. Gửi email xác nhận
            try
            {
                await _emailService.SendBookingConfirmationEmailAsync(booking.BookingId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send confirmation email: {ex.Message}");
            }

            // 12. Return response
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
                PaymentStatus = "CheckedIn",
                BookingType = "WalkIn",
                SpecialRequests = request.SpecialRequests,
                CreatedAt = booking.CreatedAt
            };

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo booking tại quầy thành công!",
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

            // Update customer info
            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            if (customer != null)
            {
                if (!string.IsNullOrEmpty(request.FullName)) customer.FullName = request.FullName;
                if (!string.IsNullOrEmpty(request.PhoneNumber)) customer.PhoneNumber = request.PhoneNumber;
                if (!string.IsNullOrEmpty(request.IdentityCard)) customer.IdentityCard = request.IdentityCard;
                if (!string.IsNullOrEmpty(request.Address)) customer.Address = request.Address;
                
                await _unitOfWork.Customers.UpdateAsync(customer);
            }

            // Update booking info
            if (request.CheckInDate.HasValue) booking.CheckInDate = request.CheckInDate.Value;
            if (request.CheckOutDate.HasValue) booking.CheckOutDate = request.CheckOutDate.Value;
            if (!string.IsNullOrEmpty(request.SpecialRequests)) booking.SpecialRequests = request.SpecialRequests;
            
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
                Message = $"Lỗi: {ex.Message}"
            };
        }
    }


    public async Task<ResultModel> GetOfflineBookingsAsync(OfflineBookingFilterRequest filter)
    {
        try
        {
            var walkInType = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == "WalkIn")).FirstOrDefault();

            if (walkInType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Không tìm thấy booking type WalkIn"
                };
            }

            var query = await _unitOfWork.Bookings.FindAsync(b => b.BookingTypeId == walkInType.CodeId);
            
            if (filter.FromDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt >= filter.FromDate.Value);
            }
            
            if (filter.ToDate.HasValue)
            {
                query = query.Where(b => b.CreatedAt <= filter.ToDate.Value);
            }
            
            if (filter.BookingStatus.HasValue)
            {
                query = query.Where(b => b.StatusId == filter.BookingStatus.Value);
            }

            var bookings = query.OrderByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var bookingDtos = new List<BookingDto>();
            foreach (var booking in bookings)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
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

                bookingDtos.Add(new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = booking.CustomerId,
                    CustomerName = customer?.FullName ?? "",
                    RoomIds = rooms.Select(r => r.RoomId).ToList(),
                    RoomNames = rooms.Select(r => r.RoomName).ToList(),
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentStatus = statusCode?.CodeValue ?? "Unknown",
                    BookingType = "WalkIn",
                    SpecialRequests = booking.SpecialRequests,
                    CreatedAt = booking.CreatedAt
                });
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Data = new
                {
                    Bookings = bookingDtos,
                    TotalCount = query.Count(),
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
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

    public async Task<ResultModel> GetBookingDetailAsync(int bookingId)
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
                if (room != null) rooms.Add(room);
            }

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

            var detail = new BookingDetailDto
            {
                BookingId = booking.BookingId,
                CustomerId = booking.CustomerId,
                CustomerName = customer?.FullName ?? "",
                CustomerEmail = customer?.Account?.Email ?? "",
                CustomerPhone = customer?.PhoneNumber ?? "",
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
                Data = detail
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

    public async Task<ResultModel> CancelBookingAsync(int bookingId, CancelBookingRequest request, int employeeId)
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

            var cancelledStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" && c.CodeName == "Cancelled")).FirstOrDefault();

            if (cancelledStatus == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Không tìm thấy status Cancelled"
                };
            }

            booking.StatusId = cancelledStatus.CodeId;
            booking.UpdatedAt = DateTime.UtcNow;
            booking.UpdatedBy = employeeId;
            
            await _unitOfWork.Bookings.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Đã hủy booking thành công"
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
    /// Nhân viên chỉ cần BookingId để hiển thị QR cho khách
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

            // Kiểm tra status - chỉ generate QR cho booking Pending
            var statusCode = booking.StatusId.HasValue
                ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                : null;

            if (statusCode?.CodeName != "Pending")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Booking đã ở trạng thái {statusCode?.CodeValue}. Không thể tạo QR thanh toán."
                };
            }

            // Lấy transaction ref từ cache hoặc generate mới
            var cacheKey = bookingId.ToString();
            var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, cacheKey);
            
            string transactionRef;
            if (paymentInfo != null && paymentInfo.TransactionRef != null)
            {
                transactionRef = paymentInfo.TransactionRef.ToString();
            }
            else
            {
                transactionRef = _qrPaymentHelper.GenerateTransactionRef(bookingId);
            }

            // Lấy bank config
            var bankConfig = (await _unitOfWork.BankConfigs.FindAsync(bc => bc.IsActive)).FirstOrDefault();
            if (bankConfig == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "Chưa cấu hình thông tin ngân hàng"
                };
            }

            var (isValid, errorMessage) = _qrPaymentHelper.ValidateBankConfig(bankConfig);
            if (!isValid)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Cấu hình ngân hàng không hợp lệ: {errorMessage}"
                };
            }

            // Generate QR
            var description = $"Dat coc booking {bookingId}";
            var qrUrl = _qrPaymentHelper.GenerateVietQRUrl(bankConfig, booking.DepositAmount, description, transactionRef);
            var qrDataText = _qrPaymentHelper.GenerateQRData(bankConfig, booking.DepositAmount, description);

            var qrPaymentInfoDto = new QRPaymentInfoDto
            {
                QRCodeUrl = qrUrl,
                BankName = bankConfig.BankName,
                BankCode = bankConfig.BankCode,
                AccountNumber = bankConfig.AccountNumber,
                AccountName = bankConfig.AccountName,
                Amount = booking.DepositAmount,
                Description = description,
                TransactionRef = transactionRef,
                QRDataText = qrDataText
            };

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
                    QRPayment = qrPaymentInfoDto,
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

    /// <summary>
    /// Search và filter phòng available với đầy đủ tiêu chí
    /// </summary>
    public async Task<ResultModel> SearchAvailableRoomsAsync(SearchAvailableRoomsRequest request)
    {
        try
        {
            // 1. Query base - lấy tất cả phòng active
            var roomsQuery = (await _unitOfWork.Rooms.GetAllAsync())
                .Where(r => r.RoomType.IsActive);

            // 2. Filter theo RoomType
            if (request.RoomTypeId.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomTypeId == request.RoomTypeId.Value);
            }

            // 3. Filter theo số giường
            if (request.NumberOfBeds.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.NumberOfBeds == request.NumberOfBeds.Value);
            }

            // 4. Filter theo loại giường
            if (!string.IsNullOrEmpty(request.BedType))
            {
                roomsQuery = roomsQuery.Where(r => 
                    r.RoomType.BedType != null && 
                    r.RoomType.BedType.Contains(request.BedType, StringComparison.OrdinalIgnoreCase));
            }

            // 5. Filter theo số người tối đa
            if (request.MaxOccupancy.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.MaxOccupancy >= request.MaxOccupancy.Value);
            }

            // 6. Filter theo giá
            if (request.MinPrice.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.BasePriceNight >= request.MinPrice.Value);
            }
            if (request.MaxPrice.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => r.RoomType.BasePriceNight <= request.MaxPrice.Value);
            }

            // 7. Filter theo diện tích
            if (request.MinRoomSize.HasValue)
            {
                roomsQuery = roomsQuery.Where(r => 
                    r.RoomType.RoomSize.HasValue && 
                    r.RoomType.RoomSize.Value >= request.MinRoomSize.Value);
            }

            // 8. Search theo tên hoặc mã phòng
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                roomsQuery = roomsQuery.Where(r => 
                    r.RoomName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.RoomType.TypeName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.RoomType.TypeCode.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // 9. Filter theo ngày available
            if (request.CheckInDate.HasValue && request.CheckOutDate.HasValue)
            {
                var availableRoomIds = new List<int>();
                foreach (var room in roomsQuery)
                {
                    var isAvailable = await _bookingHelper.IsRoomAvailableAsync(
                        room.RoomId,
                        request.CheckInDate.Value,
                        request.CheckOutDate.Value
                    );
                    if (isAvailable)
                    {
                        availableRoomIds.Add(room.RoomId);
                    }
                }
                roomsQuery = roomsQuery.Where(r => availableRoomIds.Contains(r.RoomId));
            }

            // 10. Sorting
            roomsQuery = request.SortBy?.ToLower() switch
            {
                "price" => request.IsDescending 
                    ? roomsQuery.OrderByDescending(r => r.RoomType.BasePriceNight)
                    : roomsQuery.OrderBy(r => r.RoomType.BasePriceNight),
                "roomsize" => request.IsDescending
                    ? roomsQuery.OrderByDescending(r => r.RoomType.RoomSize ?? 0)
                    : roomsQuery.OrderBy(r => r.RoomType.RoomSize ?? 0),
                "roomname" => request.IsDescending
                    ? roomsQuery.OrderByDescending(r => r.RoomName)
                    : roomsQuery.OrderBy(r => r.RoomName),
                _ => roomsQuery.OrderBy(r => r.RoomId)
            };

            // 11. Pagination
            var totalCount = roomsQuery.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var rooms = roomsQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // 12. Build response DTOs
            var roomDtos = new List<AvailableRoomDto>();
            foreach (var room in rooms)
            {
                var roomType = room.RoomType;
                var statusCode = await _unitOfWork.CommonCodes.GetByIdAsync(room.StatusId);

                // Lấy amenities
                var roomAmenities = await _unitOfWork.RoomAmenities.FindAsync(ra => ra.RoomId == room.RoomId);
                var amenityNames = new List<string>();
                foreach (var ra in roomAmenities)
                {
                    var amenity = await _unitOfWork.Amenities.GetByIdAsync(ra.AmenityId);
                    if (amenity != null)
                    {
                        amenityNames.Add(amenity.AmenityName);
                    }
                }

                // Lấy images - Sửa lỗi: Medium có FilePath không phải Url
                var media = await _unitOfWork.Mediums.FindAsync(m => 
                    m.ReferenceTable == "Room" && 
                    m.ReferenceKey == room.RoomId.ToString() && 
                    m.IsActive);
                var imageUrls = media.Select(m => m.FilePath).ToList();

                roomDtos.Add(new AvailableRoomDto
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    RoomTypeId = roomType.RoomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    PricePerNight = roomType.BasePriceNight,
                    MaxOccupancy = roomType.MaxOccupancy,
                    RoomSize = roomType.RoomSize,
                    NumberOfBeds = roomType.NumberOfBeds,
                    BedType = roomType.BedType,
                    Description = roomType.Description,
                    Status = statusCode?.CodeValue ?? "Unknown",
                    Amenities = amenityNames,
                    Images = imageUrls
                });
            }

            var response = new AvailableRoomsResponse
            {
                Rooms = roomDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Tìm thấy {totalCount} phòng phù hợp",
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
    /// Manager/Admin xác nhận đã nhận được tiền cọc từ khách (sau khi check bill ngân hàng)
    /// Chuyển status từ PendingConfirmation → Confirmed
    /// Gửi email cảm ơn + thông tin đặt phòng chi tiết cho khách
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

            // Kiểm tra status hiện tại - chỉ confirm được booking PendingConfirmation
            var currentStatus = booking.StatusId.HasValue
                ? await _unitOfWork.CommonCodes.GetByIdAsync(booking.StatusId.Value)
                : null;

            if (currentStatus?.CodeName != "PendingConfirmation")
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Không thể confirm booking ở trạng thái {currentStatus?.CodeValue}. Chỉ có thể confirm booking đang ở trạng thái 'Chờ xác nhận'."
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
                if (room != null && room.RoomType != null)
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
            await _unitOfWork.SaveChangesAsync();

            // Gửi email cảm ơn + thông tin đặt phòng chi tiết cho khách hàng
            try
            {
                await _emailService.SendBookingConfirmationEmailAsync(booking.BookingId);
            }
            catch (Exception emailEx)
            {
                // Log lỗi nhưng không fail
                // Email sẽ được gửi lại sau nếu cần
                Console.WriteLine($"[ConfirmOnlineBooking] Failed to send email: {emailEx.Message}");
            }

            return new ResultModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xác nhận thanh toán thành công. Email xác nhận đã được gửi đến khách hàng."
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
