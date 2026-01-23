using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.CheckoutServices
{
    /// <summary>
    /// Service xử lý checkout - thanh toán và hoàn tất booking
    /// </summary>
    public class CheckoutService : ICheckoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HotelManagementContext _context;
        private readonly ILogger<CheckoutService> _logger;

        public CheckoutService(
            IUnitOfWork unitOfWork,
            HotelManagementContext context,
            ILogger<CheckoutService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Xem trước hóa đơn checkout (không lưu database)
        /// </summary>
        public async Task<ResultModel> PreviewCheckoutAsync(PreviewCheckoutRequest request)
        {
            try
            {
                // 1. Load booking với all relationships
                var booking = await LoadBookingWithDetailsAsync(request.BookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // 2. Kiểm tra trạng thái booking
                if (booking.Status?.CodeName == "Cancelled")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Booking đã bị hủy"
                    };
                }

                if (booking.Status?.CodeName == "Completed")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Booking đã được checkout"
                    };
                }

                // 3. Sử dụng CheckOutDate từ booking
                var checkoutDate = booking.CheckOutDate;
                
                // CRITICAL FIX: Phải dùng CodeName (English) thay vì CodeValue (Tiếng Việt)
                bool isOnlineBooking = booking.BookingType?.CodeName == "Online";
                
                // Debug logging
                _logger.LogInformation("PreviewCheckout - BookingId: {BookingId}, BookingType CodeName: {CodeName}, CodeValue: {CodeValue}, IsOnline: {IsOnline}, DepositAmount: {Deposit}",
                    booking.BookingId,
                    booking.BookingType?.CodeName ?? "null",
                    booking.BookingType?.CodeValue ?? "null",
                    isOnlineBooking,
                    booking.DepositAmount);

                // 4. Tính tiền phòng
                var roomCharges = await CalculateRoomChargesAsync(booking, checkoutDate, isOnlineBooking);
                decimal totalRoomCharges = roomCharges.Sum(r => r.SubTotal);

                // 5. Tính tiền dịch vụ
                var serviceCharges = await CalculateServiceChargesAsync(booking);
                decimal totalServiceCharges = serviceCharges.Sum(s => s.SubTotal);

                // 6. Tính tổng
                decimal subTotal = totalRoomCharges + totalServiceCharges;
                
                // FIX: Calculate actual deposit paid from existing transactions
                decimal depositPaid = 0;
                
                // Check if there are any existing transactions (deposit payment)
                var existingTransactions = await _context.Transactions
                    .Where(t => t.BookingId == booking.BookingId)
                    .ToListAsync();
                
                if (existingTransactions.Any())
                {
                    // Sum up all paid amounts from existing transactions
                    depositPaid = existingTransactions.Sum(t => t.PaidAmount);
                    _logger.LogInformation("Found existing transactions - Total Paid: {Deposit}", depositPaid);
                }
                else if (isOnlineBooking)
                {
                    // If no transaction but online booking, use booking's DepositAmount or calculate 30%
                    depositPaid = booking.DepositAmount > 0 ? booking.DepositAmount : (subTotal * 0.3m);
                    _logger.LogInformation("Online Booking - Calculated Deposit: {Deposit} (from DB: {DBDeposit})", 
                        depositPaid, booking.DepositAmount);
                }
                
                decimal totalAmount = subTotal;
                decimal amountDue = totalAmount - depositPaid;

                var totalNights = CalculateNights(booking.CheckInDate, booking.CheckOutDate);

                var response = new PreviewCheckoutResponse
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType?.CodeValue ?? "Unknown",
                    BookingTypeCode = booking.BookingType?.CodeName ?? "Unknown",
                    BookingStatus = booking.Status.CodeValue ?? "Unknown",
                    BookingStatusCode = booking.Status.CodeName ?? "Unknown",
                    Customer = new CustomerCheckoutInfo
                    {
                        CustomerId = booking.Customer.CustomerId,
                        FullName = booking.Customer.FullName,
                        Email = booking.Customer.Account?.Email ?? "",
                        PhoneNumber = booking.Customer.PhoneNumber,
                        IdentityCard = booking.Customer.IdentityCard
                    },
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalNights = totalNights,
                    RoomCharges = roomCharges,
                    TotalRoomCharges = totalRoomCharges,
                    ServiceCharges = serviceCharges,
                    TotalServiceCharges = totalServiceCharges,
                    SubTotal = subTotal,
                    DepositPaid = depositPaid,
                    TotalAmount = totalAmount,
                    AmountDue = amountDue
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Preview checkout thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi preview checkout booking {BookingId}", request.BookingId);
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Xử lý checkout và thanh toán hoàn tất
        /// Lưu ý: Checkout theo đúng ngày CheckOutDate trong booking
        /// Nếu khách muốn ở thêm, cần tạo booking mới trước khi checkout
        /// Cho phép checkout sớm nếu khách muốn trả phòng trước ngày dự kiến
        /// </summary>
        public async Task<ResultModel> ProcessCheckoutAsync(CheckoutRequest request, int? processedBy = null)
        {
            try
            {
                // 1. Load booking với all relationships
                var booking = await LoadBookingWithDetailsAsync(request.BookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // 2. Kiểm tra ngày checkout: Cho phép checkout sớm
                var today = DateTime.Now.Date;
                var checkOutDate = booking.CheckOutDate.Date;
                var checkInDate = booking.CheckInDate.Date;
                
                // Không cho phép checkout trước ngày check-in
                if (today < checkInDate)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = $"Không thể checkout trước ngày check-in: {checkInDate:dd/MM/yyyy}. Hôm nay: {today:dd/MM/yyyy}."
                    };
                }
                
                // Xác định ngày checkout thực tế (cho phép checkout sớm)
                var actualCheckOutDate = today < checkOutDate ? today : checkOutDate;
                bool isEarlyCheckout = today < checkOutDate;
                
                if (isEarlyCheckout)
                {
                    _logger.LogInformation("Early checkout - BookingId: {BookingId}, PlannedCheckout: {Planned}, ActualCheckout: {Actual}",
                        booking.BookingId, checkOutDate, actualCheckOutDate);
                }

                // 3. Validate booking status
                if (booking.Status?.CodeName == "Cancelled")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Booking đã bị hủy, không thể checkout"
                    };
                }

                if (booking.Status?.CodeName == "Completed")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Booking đã được checkout trước đó"
                    };
                }

                if (booking.Status?.CodeName == "Pending" || booking.Status?.CodeName == "PendingConfirmation")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Booking chưa được xác nhận, không thể checkout"
                    };
                }

                bool isOnlineBooking = booking.BookingType?.CodeName == "Online";

                // 3. Tính tiền phòng (sử dụng CheckOutDate từ booking)
                var roomCharges = await CalculateRoomChargesAsync(booking, booking.CheckOutDate, isOnlineBooking);
                decimal totalRoomCharges = roomCharges.Sum(r => r.SubTotal);

                // 4. Tính tiền dịch vụ (bao gồm TẤT CẢ services đã add trong quá trình ở)
                // - BookingRoomService: Dịch vụ theo phòng (giặt ủi, minibar, v.v.)
                // - BookingService: Dịch vụ chung (spa, massage, v.v.)
                var serviceCharges = await CalculateServiceChargesAsync(booking);
                decimal totalServiceCharges = serviceCharges.Sum(s => s.SubTotal);

                // 5. Tính tổng cần thanh toán
                decimal subTotal = totalRoomCharges + totalServiceCharges;

                // Nếu là booking ONLINE: Đã trả 30% deposit trước → trừ ra
                // Nếu là booking OFFLINE: Không có deposit → thanh toán full
                // CRITICAL: Nếu booking.DepositAmount chưa được set, tính lại
                decimal depositPaid = 0;
                if (isOnlineBooking)
                {
                    depositPaid = booking.DepositAmount > 0 ? booking.DepositAmount : (subTotal * 0.3m);
                }
                
                decimal totalAmount = subTotal;
                decimal amountDue = totalAmount - depositPaid; // Số tiền còn phải trả tại quầy

                // 6. Lấy CommonCode IDs
                var paymentMethod = await _unitOfWork.CommonCodes.GetByIdAsync(request.PaymentMethodId);
                if (paymentMethod == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Phương thức thanh toán không hợp lệ"
                    };
                }

                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeName == "Paid")).FirstOrDefault();
                var completedTransactionStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();
                var completedBookingStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && c.CodeName == "Completed")).FirstOrDefault();
                var availableRoomStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "RoomStatus" && c.CodeName == "Available")).FirstOrDefault();
                
                if (paidStatus == null || completedTransactionStatus == null ||
                    completedBookingStatus == null || availableRoomStatus == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "Lỗi cấu hình hệ thống: Thiếu status codes"
                    };
                }

                // 7. Tạo transaction cho CHECKOUT (ghi nhận TOÀN BỘ thanh toán của booking này)
                // QUAN TRỌNG: 
                // - TotalAmount: Tổng hóa đơn (phòng + dịch vụ)
                // - PaidAmount: TOÀN BỘ số tiền đã thanh toán (bao gồm deposit nếu online + số tiền trả tại quầy)
                // - DepositAmount: Số tiền deposit đã trả trước (nếu online booking)
                // 
                // Lý do: Dashboard tính doanh thu bằng SUM(PaidAmount) của TẤT CẢ transactions
                // Nếu chỉ ghi amountDue (70%), dashboard sẽ thiếu 30% deposit!
                var transaction = new Transaction
                {
                    BookingId = booking.BookingId,
                    TotalAmount = totalAmount, // Tổng hóa đơn (room + services)
                    PaidAmount = totalAmount, // TOÀN BỘ số tiền khách đã thanh toán (100%)
                    PaymentMethodId = request.PaymentMethodId,
                    PaymentStatusId = paidStatus.CodeId,
                    TransactionStatusId = completedTransactionStatus.CodeId,
                    DepositAmount = depositPaid, // Số tiền deposit đã trả trước (30% nếu online, 0 nếu offline)
                    DepositStatusId = depositPaid > 0 ? paidStatus.CodeId : null,
                    DepositDate = depositPaid > 0 ? booking.CreatedAt : null, // Ngày trả deposit (lúc tạo booking)
                    TransactionRef = request.TransactionReference ?? $"CHECKOUT-{booking.BookingId}-{DateTime.UtcNow.Ticks}",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = processedBy, // Lễ tân xử lý checkout
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = processedBy
                };

                await _unitOfWork.Transactions.AddAsync(transaction);

                // 8. Cập nhật booking status = Completed
                booking.StatusId = completedBookingStatus.CodeId;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = processedBy;

                await _unitOfWork.Bookings.UpdateAsync(booking);

                // 9. Cập nhật room status = Available
                foreach (var bookingRoom in booking.BookingRooms)
                {
                    var room = await _unitOfWork.Rooms.GetByIdAsync(bookingRoom.RoomId);
                    if (room != null)
                    {
                        room.StatusId = availableRoomStatus.CodeId;
                        await _unitOfWork.Rooms.UpdateAsync(room);
                    }
                }

                // 10. Save all changes
                await _unitOfWork.SaveChangesAsync();

                // 11. Prepare response
                var totalNights = CalculateNights(booking.CheckInDate, booking.CheckOutDate);
                var response = new CheckoutResponse
                {
                    BookingId = booking.BookingId,
                    BookingType = booking.BookingType?.CodeValue ?? "Unknown",
                    BookingTypeCode = booking.BookingType?.CodeName ?? "Unknown",
                    Customer = new CustomerCheckoutInfo
                    {
                        CustomerId = booking.Customer.CustomerId,
                        FullName = booking.Customer.FullName,
                        Email = booking.Customer.Account?.Email ?? "",
                        PhoneNumber = booking.Customer.PhoneNumber,
                        IdentityCard = booking.Customer.IdentityCard
                    },
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    ActualCheckOutDate = booking.CheckOutDate, // Sử dụng CheckOutDate từ booking
                    TotalNights = totalNights,
                    ActualNights = totalNights,
                    RoomCharges = roomCharges,
                    TotalRoomCharges = totalRoomCharges,
                    ServiceCharges = serviceCharges,
                    TotalServiceCharges = totalServiceCharges,
                    SubTotal = subTotal,
                    DepositPaid = depositPaid,
                    TotalAmount = totalAmount,
                    AmountDue = amountDue,
                    PaymentMethod = paymentMethod.CodeName,
                    TransactionId = transaction.TransactionId,
                    CheckoutProcessedAt = DateTime.UtcNow,
                    ProcessedBy = processedBy?.ToString()
                };

                _logger.LogInformation("Checkout thành công booking {BookingId}, tổng tiền {TotalAmount}",
                    booking.BookingId, totalAmount);

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Checkout thành công!",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi checkout booking {BookingId}", request.BookingId);
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi khi checkout: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy thông tin booking để chuẩn bị checkout
        /// </summary>
        public async Task<ResultModel> GetBookingForCheckoutAsync(int bookingId)
        {
            try
            {
                var booking = await LoadBookingWithDetailsAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Không tìm thấy booking"
                    };
                }

                // Map to DTO
                var response = new
                {
                    BookingId = booking.BookingId,
                    Customer = new
                    {
                        booking.Customer.CustomerId,
                        booking.Customer.FullName,
                        Email = booking.Customer.Account?.Email ?? "",
                        booking.Customer.PhoneNumber,
                        booking.Customer.IdentityCard
                    },
                    Rooms = booking.BookingRooms.Select(br => new
                    {
                        br.BookingRoomId,
                        br.RoomId,
                        RoomName = br.Room.RoomName,
                        RoomTypeName = br.Room.RoomType.TypeName,
                        br.PricePerNight,
                        br.NumberOfNights,
                        br.SubTotal,
                        Services = br.BookingRoomServices.Select(brs => new
                        {
                            brs.ServiceId,
                            ServiceName = brs.Service.ServiceName,
                            brs.PriceAtTime,
                            brs.Quantity,
                            SubTotal = brs.PriceAtTime * brs.Quantity
                        }).ToList()
                    }).ToList(),
                    BookingServices = booking.BookingServices.Select(bs => new
                    {
                        bs.ServiceId,
                        ServiceName = bs.Service.ServiceName,
                        bs.PriceAtTime,
                        bs.Quantity,
                        bs.TotalPrice,
                        bs.ServiceDate
                    }).ToList(),
                    Transactions = booking.Transactions.Select(t => new
                    {
                        t.TransactionId,
                        t.TotalAmount,
                        t.PaidAmount,
                        PaymentMethod = t.PaymentMethod.CodeName,
                        PaymentStatus = t.PaymentStatus.CodeName,
                        t.TransactionRef,
                        t.CreatedAt
                    }).ToList(),
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.TotalAmount,
                    booking.DepositAmount,
                    BookingType = booking.BookingType?.CodeValue,
                    Status = booking.Status?.CodeName,
                    booking.SpecialRequests,
                    booking.CreatedAt
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Lấy thông tin booking thành công",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy booking {BookingId} để checkout", bookingId);
                return new ResultModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        /// <summary>
        /// Load booking với tất cả relationships cần thiết
        /// </summary>
        private async Task<Booking?> LoadBookingWithDetailsAsync(int bookingId)
        {
            var bookings = await _unitOfWork.Bookings.FindAsync(
                b => b.BookingId == bookingId,
                b => b.Customer,
                b => b.Customer.Account,
                b => b.BookingRooms,
                b => b.BookingServices,
                b => b.Transactions,
                b => b.Status,
                b => b.BookingType
            );

            var booking = bookings.FirstOrDefault();
            if (booking == null) return null;

            // Load thêm các relationships phức tạp
            foreach (var bookingRoom in booking.BookingRooms)
            {
                var room = await _unitOfWork.Rooms.GetSingleAsync(
                    r => r.RoomId == bookingRoom.RoomId,
                    r => r.RoomType
                );

                if (room != null)
                {
                    bookingRoom.Room = room;
                }

                // Load BookingRoomServices
                var roomServices = await _context.BookingRoomServices
                    .Where(brs => brs.BookingRoomId == bookingRoom.BookingRoomId)
                    .ToListAsync();

                foreach (var roomService in roomServices)
                {
                    var service = await _context.Services.FindAsync(roomService.ServiceId);
                    if (service != null)
                    {
                        roomService.Service = service;
                    }
                }

                bookingRoom.BookingRoomServices = roomServices.ToList();
            }

            // Load services
            foreach (var bookingService in booking.BookingServices)
            {
                var service = await _context.Services.FindAsync(bookingService.ServiceId);
                if (service != null)
                {
                    bookingService.Service = service;
                }
            }

            // Load transaction details
            foreach (var transaction in booking.Transactions)
            {
                if (transaction.PaymentMethodId > 0)
                {
                    transaction.PaymentMethod = await _unitOfWork.CommonCodes.GetByIdAsync(transaction.PaymentMethodId) ?? transaction.PaymentMethod;
                }
                if (transaction.PaymentStatusId > 0)
                {
                    transaction.PaymentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(transaction.PaymentStatusId) ?? transaction.PaymentStatus;
                }
            }

            return booking;
        }

        /// <summary>
        /// Tính tiền phòng theo booking type
        /// </summary>
        private async Task<List<RoomChargeDetail>> CalculateRoomChargesAsync(
            Booking booking,
            DateTime actualCheckOutDate,
            bool isOnlineBooking)
        {
            var roomCharges = new List<RoomChargeDetail>();

            foreach (var bookingRoom in booking.BookingRooms)
            {
                int actualNights;
                decimal subTotal;

                if (isOnlineBooking)
                {
                    // Online: Dùng số đêm đã book trước (không tính lại)
                    actualNights = bookingRoom.NumberOfNights;
                    subTotal = bookingRoom.SubTotal; // Đã tính sẵn
                }
                else
                {
                    // Offline: Tính theo ngày thực tế
                    actualNights = CalculateNights(bookingRoom.CheckInDate, actualCheckOutDate);
                    subTotal = bookingRoom.PricePerNight * actualNights;
                }

                roomCharges.Add(new RoomChargeDetail
                {
                    BookingRoomId = bookingRoom.BookingRoomId,
                    RoomId = bookingRoom.RoomId,
                    RoomName = bookingRoom.Room.RoomName,
                    RoomTypeName = bookingRoom.Room.RoomType.TypeName, // CodeValue - Hiển thị
                    RoomTypeCode = bookingRoom.Room.RoomType.TypeCode ?? "", // CodeName - Logic
                    PricePerNight = bookingRoom.PricePerNight,
                    PlannedNights = bookingRoom.NumberOfNights,
                    ActualNights = actualNights,
                    SubTotal = subTotal,
                    CheckInDate = bookingRoom.CheckInDate,
                    CheckOutDate = bookingRoom.CheckOutDate
                });
            }

            return roomCharges;
        }

        /// <summary>
        /// Tính tổng tiền dịch vụ đã sử dụng trong quá trình ở
        /// Bao gồm TẤT CẢ services được add trước khi checkout
        /// </summary>
        private async Task<List<ServiceChargeDetail>> CalculateServiceChargesAsync(Booking booking)
        {
            var serviceCharges = new List<ServiceChargeDetail>();

            // ===== DỊCH VỤ THEO PHÒNG (BookingRoomService) =====
            // VD: Giặt ủi, minibar, room service, late checkout phí theo phòng
            // Services được add bởi nhân viên trong quá trình khách ở
            foreach (var bookingRoom in booking.BookingRooms)
            {
                foreach (var roomService in bookingRoom.BookingRoomServices)
                {
                    serviceCharges.Add(new ServiceChargeDetail
                    {
                        ServiceId = roomService.ServiceId,
                        ServiceName = roomService.Service.ServiceName, // Hiển thị
                        ServiceCode = roomService.Service.ServiceName, // Logic (Service không có code riêng)
                        PricePerUnit = roomService.PriceAtTime,
                        Quantity = roomService.Quantity,
                        SubTotal = roomService.PriceAtTime * roomService.Quantity,
                        ServiceDate = DateTime.UtcNow, // BookingRoomService không có ServiceDate
                        ServiceType = "RoomService",
                        RoomName = bookingRoom.Room.RoomName
                    });
                }
            }

            // ===== DỊCH VỤ CHUNG (BookingService) =====
            // VD: Spa, massage, ăn uống, tour, dịch vụ không gắn với phòng cụ thể
            // Services được add bởi nhân viên trong quá trình khách ở
            foreach (var bookingService in booking.BookingServices)
            {
                serviceCharges.Add(new ServiceChargeDetail
                {
                    ServiceId = bookingService.ServiceId,
                    ServiceName = bookingService.Service.ServiceName, // Hiển thị
                    ServiceCode = bookingService.Service.ServiceName, // Logic (Service không có code riêng)
                    PricePerUnit = bookingService.PriceAtTime,
                    Quantity = bookingService.Quantity,
                    SubTotal = bookingService.TotalPrice,
                    ServiceDate = bookingService.ServiceDate,
                    ServiceType = "BookingService",
                    RoomName = null
                });
            }

            return serviceCharges;
        }

        /// <summary>
        /// Tính số đêm giữa 2 ngày
        /// </summary>
        private int CalculateNights(DateTime checkIn, DateTime checkOut)
        {
            var nights = (checkOut.Date - checkIn.Date).Days;
            return nights > 0 ? nights : 1; // Ít nhất 1 đêm
        }
    }
}
