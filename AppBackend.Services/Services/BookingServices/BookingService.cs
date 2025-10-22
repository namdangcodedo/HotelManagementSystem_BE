using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Helpers;
using AppBackend.Services.MessageQueue;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using BusinessTransaction = AppBackend.BusinessObjects.Models.Transaction;

namespace AppBackend.Services.Services.BookingServices
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly IBookingQueueService _queueService;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;

        public BookingService(
            IUnitOfWork unitOfWork,
            CacheHelper cacheHelper,
            IBookingQueueService queueService,
            PayOS payOS,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _queueService = queueService;
            _payOS = payOS;
            _configuration = configuration;
        }

        public async Task<ResultModel> CheckRoomAvailabilityAsync(CheckRoomAvailabilityRequest request)
        {
            var availabilityResults = new List<RoomTypeAvailabilityDto>();

            foreach (var roomTypeQty in request.RoomTypeQuantities)
            {
                int roomTypeId = roomTypeQty.Key;
                int requestedQuantity = roomTypeQty.Value;

                // Lấy thông tin RoomType
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Loại phòng ID {roomTypeId} không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // Lấy tất cả phòng thuộc loại này
                var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
                
                // Lấy status "Available" để filter
                var availableStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "RoomStatus" && c.CodeName == "Available")).FirstOrDefault();

                var availableRooms = new List<Room>();

                foreach (var room in allRoomsOfType)
                {
                    // Kiểm tra trong cache xem phòng có đang bị lock không
                    var lockKey = $"{room.RoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);

                    if (!string.IsNullOrEmpty(lockedBy))
                    {
                        continue; // Phòng đang bị lock, bỏ qua
                    }

                    // Kiểm tra trong database xem phòng có đang được đặt không
                    var existingBookings = await _unitOfWork.BookingRooms.FindAsync(br =>
                        br.RoomId == room.RoomId &&
                        br.Booking.CheckInDate < request.CheckOutDate &&
                        br.Booking.CheckOutDate > request.CheckInDate);

                    if (!existingBookings.Any())
                    {
                        availableRooms.Add(room);
                    }
                }

                availabilityResults.Add(new RoomTypeAvailabilityDto
                {
                    RoomTypeId = roomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    BasePriceNight = roomType.BasePriceNight,
                    MaxOccupancy = roomType.MaxOccupancy,
                    AvailableCount = availableRooms.Count,
                    RequestedQuantity = requestedQuantity,
                    IsAvailable = availableRooms.Count >= requestedQuantity
                });
            }

            // Kiểm tra xem tất cả loại phòng đều đủ số lượng không
            var unavailableTypes = availabilityResults.Where(r => !r.IsAvailable).ToList();
            if (unavailableTypes.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Một số loại phòng không đủ số lượng trong khoảng thời gian này",
                    Data = availabilityResults,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Tất cả loại phòng đều có đủ số lượng",
                Data = availabilityResults,
                StatusCode = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Tìm và chọn phòng available theo loại phòng
        /// </summary>
        private async Task<List<Room>> FindAvailableRoomsByTypeAsync(int roomTypeId, int quantity, DateTime checkInDate, DateTime checkOutDate)
        {
            // Lấy tất cả phòng thuộc loại này
            var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            
            var availableRooms = new List<Room>();

            foreach (var room in allRoomsOfType)
            {
                if (availableRooms.Count >= quantity)
                    break; // Đã đủ số lượng

                // Kiểm tra trong cache xem phòng có đang bị lock không
                var lockKey = $"{room.RoomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
                var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);

                if (!string.IsNullOrEmpty(lockedBy))
                {
                    continue; // Phòng đang bị lock, bỏ qua
                }

                // Kiểm tra trong database xem phòng có đang được đặt không
                var existingBookings = await _unitOfWork.BookingRooms.FindAsync(br =>
                    br.RoomId == room.RoomId &&
                    br.Booking.CheckInDate < checkOutDate &&
                    br.Booking.CheckOutDate > checkInDate);

                if (!existingBookings.Any())
                {
                    availableRooms.Add(room);
                }
            }

            return availableRooms;
        }

        /// <summary>
        /// Tính giá phòng có áp dụng điều chỉnh theo ngày lễ
        /// </summary>
        private async Task<decimal> CalculateRoomPriceWithHolidayAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, decimal basePricePerNight)
        {
            decimal totalPrice = 0;
            var currentDate = checkInDate;
            var numberOfNights = (checkOutDate - checkInDate).Days;

            for (int night = 0; night < numberOfNights; night++)
            {
                var nightDate = currentDate.AddDays(night);
                var priceForNight = basePricePerNight;

                // Kiểm tra xem ngày này có trong khoảng thời gian ngày lễ nào không
                var holidayPricing = (await _unitOfWork.HolidayPricings.FindAsync(hp =>
                    hp.IsActive &&
                    hp.RoomId == roomId &&
                    hp.StartDate <= nightDate &&
                    hp.EndDate >= nightDate
                )).FirstOrDefault();

                if (holidayPricing != null)
                {
                    // Áp dụng điều chỉnh giá theo đêm
                    priceForNight = basePricePerNight + holidayPricing.PriceAdjustment;
                }

                totalPrice += priceForNight;
            }

            return totalPrice;
        }

        public async Task<ResultModel> CreateBookingAsync(CreateBookingRequest request, int userId)
        {
            // 1. Validate customer
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId);
            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Khách hàng không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. Validate RoomTypeQuantities
            if (request.RoomTypeQuantities == null || !request.RoomTypeQuantities.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Vui lòng chọn ít nhất một loại phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 3. Generate unique lock ID
            var lockId = Guid.NewGuid().ToString();

            // 4. Tìm phòng available cho từng loại phòng và lock chúng
            var selectedRooms = new List<Room>();
            var lockedRoomIds = new List<int>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();

            foreach (var roomTypeQty in request.RoomTypeQuantities)
            {
                int roomTypeId = roomTypeQty.Key;
                int quantity = roomTypeQty.Value;

                // Lấy thông tin RoomType
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    // Release all locked rooms
                    foreach (var lockedRoomId in lockedRoomIds)
                    {
                        var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                        _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Loại phòng ID {roomTypeId} không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // Tìm phòng available
                var availableRooms = await FindAvailableRoomsByTypeAsync(roomTypeId, quantity, request.CheckInDate, request.CheckOutDate);

                if (availableRooms.Count < quantity)
                {
                    // Release all locked rooms
                    foreach (var lockedRoomId in lockedRoomIds)
                    {
                        var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                        _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Không đủ phòng {roomType.TypeName}. Yêu cầu: {quantity}, Còn trống: {availableRooms.Count}",
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                // Lock các phòng đã chọn
                foreach (var room in availableRooms.Take(quantity))
                {
                    var lockKey = $"{room.RoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

                    if (!locked)
                    {
                        // Release all previously locked rooms
                        foreach (var lockedRoomId in lockedRoomIds)
                        {
                            var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                            _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                        }

                        return new ResultModel
                        {
                            IsSuccess = false,
                            Message = $"Phòng {room.RoomName} đang được đặt bởi người khác. Vui lòng thử lại!",
                            StatusCode = StatusCodes.Status409Conflict
                        };
                    }

                    lockedRoomIds.Add(room.RoomId);
                    selectedRooms.Add(room);
                }

                roomTypeDetails.Add(new RoomTypeQuantityDto
                {
                    RoomTypeId = roomTypeId,
                    RoomTypeName = roomType.TypeName,
                    Quantity = quantity,
                    PricePerNight = roomType.BasePriceNight
                });
            }

            // 5. Calculate total amount and prepare booking rooms
            decimal totalAmount = 0;
            var numberOfNights = (request.CheckOutDate - request.CheckInDate).Days;
            var bookingRoomsToAdd = new List<BookingRoom>();

            foreach (var room in selectedRooms)
            {
                // Lấy RoomType để lấy BasePriceNight
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                if (roomType == null) continue;

                // Tính giá có áp dụng điều chỉnh theo ngày lễ
                var roomSubTotal = await CalculateRoomPriceWithHolidayAsync(
                    room.RoomId, 
                    request.CheckInDate, 
                    request.CheckOutDate, 
                    roomType.BasePriceNight
                );
                
                totalAmount += roomSubTotal;

                bookingRoomsToAdd.Add(new BookingRoom
                {
                    RoomId = room.RoomId,
                    PricePerNight = roomType.BasePriceNight, // Giá gốc để tham khảo
                    NumberOfNights = numberOfNights,
                    SubTotal = roomSubTotal, // Tổng tiền đã bao gồm điều chỉnh ngày lễ
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate
                });
            }

            // 6. Create booking
            var booking = new Booking
            {
                CustomerId = request.CustomerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = totalAmount * 0.3m, // 30% deposit
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            // Get status codes
            var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
            var unpaidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "DepositStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
            var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == request.BookingType)).FirstOrDefault();

            if (unpaidStatus != null) booking.PaymentStatusId = unpaidStatus.CodeId;
            if (unpaidDepositStatus != null) booking.DepositStatusId = unpaidDepositStatus.CodeId;
            if (bookingTypeCode != null) booking.BookingTypeId = bookingTypeCode.CodeId;

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // 7. Create booking rooms (OrderDetail pattern)
            foreach (var bookingRoom in bookingRoomsToAdd)
            {
                bookingRoom.BookingId = booking.BookingId;
                await _unitOfWork.BookingRooms.AddAsync(bookingRoom);
            }
            await _unitOfWork.SaveChangesAsync();

            // 8. Create PayOS payment link
            string paymentUrl = string.Empty;
            long orderCode = 0;
            try
            {
                orderCode = long.Parse(DateTimeOffset.Now.ToString("yyMMddHHmmss"));
                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/payment/callback";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel";

                var roomNames = string.Join(", ", selectedRooms.Select(r => r.RoomName));

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)booking.DepositAmount,
                    description: $"Dat coc phong {roomNames}",
                    items: new List<ItemData>
                    {
                        new ItemData($"Booking #{booking.BookingId}", 1, (int)booking.DepositAmount)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl
                );

                var createPayment = await _payOS.createPaymentLink(paymentData);
                paymentUrl = createPayment.checkoutUrl;

                // Save payment info to cache
                _cacheHelper.Set(CachePrefix.BookingPayment, booking.BookingId.ToString(), new
                {
                    BookingId = booking.BookingId,
                    OrderCode = orderCode,
                    Amount = booking.DepositAmount,
                    LockId = lockId,
                    RoomIds = lockedRoomIds,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate
                });
            }
            catch (Exception ex)
            {
                // Rollback: Delete booking rooms first, then booking, then release locks
                var bookingRoomsToDelete = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId);
                foreach (var bookingRoom in bookingRoomsToDelete)
                {
                    await _unitOfWork.BookingRooms.DeleteAsync(bookingRoom);
                }
                
                await _unitOfWork.Bookings.DeleteAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Release room locks
                foreach (var roomId in lockedRoomIds)
                {
                    var releaseLockKey = $"{roomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                }

                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi tạo link thanh toán: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // 9. Create Transaction record
            var paymentMethodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentMethod" && c.CodeName == "EWallet")).FirstOrDefault();
            var transactionStatusCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "TransactionStatus" && c.CodeName == "Pending")).FirstOrDefault();

            if (paymentMethodCode != null && transactionStatusCode != null && unpaidStatus != null)
            {
                var transaction = new BusinessTransaction
                {
                    BookingId = booking.BookingId,
                    TotalAmount = booking.TotalAmount,
                    PaidAmount = 0,
                    DepositAmount = booking.DepositAmount,
                    PaymentMethodId = paymentMethodCode.CodeId,
                    PaymentStatusId = unpaidStatus.CodeId,
                    TransactionStatusId = transactionStatusCode.CodeId,
                    OrderCode = orderCode.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
            }

            // 10. Enqueue message to process booking
            var message = new BookingQueueMessage
            {
                MessageType = BookingMessageType.CreateBooking,
                Data = new BookingMessage
                {
                    BookingId = booking.BookingId,
                    CustomerId = customer.CustomerId,
                    RoomIds = lockedRoomIds,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalAmount = totalAmount,
                    BookingType = request.BookingType,
                    LockId = lockId
                }
            };

            await _queueService.EnqueueAsync(message);

            // 11. Schedule auto-cancel after 15 minutes if not paid
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(15));
                var paymentInfo = _cacheHelper.Get<object>(CachePrefix.BookingPayment, booking.BookingId.ToString());
                if (paymentInfo != null)
                {
                    // Payment not completed, cancel booking
                    var cancelMessage = new BookingQueueMessage
                    {
                        MessageType = BookingMessageType.CancelBooking,
                        Data = new BookingMessage
                        {
                            BookingId = booking.BookingId,
                            RoomIds = lockedRoomIds,
                            CheckInDate = request.CheckInDate,
                            CheckOutDate = request.CheckOutDate,
                            LockId = lockId
                        }
                    };
                    await _queueService.EnqueueAsync(cancelMessage);
                }
            });

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Tạo booking thành công. Vui lòng thanh toán trong 15 phút!",
                Data = new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.FullName,
                    RoomIds = lockedRoomIds,
                    RoomNames = selectedRooms.Select(r => r.RoomName).ToList(),
                    RoomTypeDetails = roomTypeDetails,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalAmount = totalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentUrl = paymentUrl,
                    CreatedAt = booking.CreatedAt
                },
                StatusCode = StatusCodes.Status201Created
            };
        }

        /// <summary>
        /// Tạo booking cho guest (không cần đăng nhập) - Tự động tạo hoặc tìm customer
        /// </summary>
        public async Task<ResultModel> CreateGuestBookingAsync(CreateGuestBookingRequest request)
        {
            // 1. Validate input
            if (string.IsNullOrWhiteSpace(request.FullName) || 
                string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Vui lòng cung cấp đầy đủ: Họ tên, Email và Số điện thoại",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 2. Validate dates
            if (request.CheckInDate <= DateTime.UtcNow)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Ngày check-in phải sau thời điểm hiện tại",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            if (request.CheckOutDate <= request.CheckInDate)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Ngày check-out phải sau ngày check-in",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Validate RoomTypeQuantities
            if (!request.RoomTypeQuantities.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Vui lòng chọn ít nhất một loại phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 3. Tìm hoặc tạo Customer (dựa trên PhoneNumber)
            var existingCustomers = await _unitOfWork.Customers.FindAsync(c => 
                c.PhoneNumber == request.PhoneNumber);

            var customer = existingCustomers.FirstOrDefault();

            if (customer == null)
            {
                // Tạo customer mới (guest không có AccountId)
                customer = new Customer
                {
                    AccountId = null,
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    IdentityCard = request.IdentityCard,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };

                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Cập nhật thông tin customer nếu có thay đổi
                bool needUpdate = false;

                if (!string.IsNullOrEmpty(request.FullName) && customer.FullName != request.FullName)
                {
                    customer.FullName = request.FullName;
                    needUpdate = true;
                }

                if (!string.IsNullOrEmpty(request.IdentityCard) && customer.IdentityCard != request.IdentityCard)
                {
                    customer.IdentityCard = request.IdentityCard;
                    needUpdate = true;
                }

                if (!string.IsNullOrEmpty(request.Address) && customer.Address != request.Address)
                {
                    customer.Address = request.Address;
                    needUpdate = true;
                }

                if (needUpdate)
                {
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Customers.UpdateAsync(customer);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // 4. Generate unique lock ID
            var lockId = Guid.NewGuid().ToString();

            // 5. Tìm phòng available cho từng loại phòng và lock chúng
            var selectedRooms = new List<Room>();
            var lockedRoomIds = new List<int>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();

            foreach (var roomTypeQty in request.RoomTypeQuantities)
            {
                int roomTypeId = roomTypeQty.Key;
                int quantity = roomTypeQty.Value;

                // Lấy thông tin RoomType
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    // Release all locked rooms
                    foreach (var lockedRoomId in lockedRoomIds)
                    {
                        var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                        _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Loại phòng ID {roomTypeId} không tồn tại",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // Tìm phòng available
                var availableRooms = await FindAvailableRoomsByTypeAsync(roomTypeId, quantity, request.CheckInDate, request.CheckOutDate);

                if (availableRooms.Count < quantity)
                {
                    // Release all locked rooms
                    foreach (var lockedRoomId in lockedRoomIds)
                    {
                        var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                        _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                    }

                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Không đủ phòng {roomType.TypeName}. Yêu cầu: {quantity}, Còn trống: {availableRooms.Count}",
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                // Lock các phòng đã chọn
                foreach (var room in availableRooms.Take(quantity))
                {
                    var lockKey = $"{room.RoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

                    if (!locked)
                    {
                        // Release all previously locked rooms
                        foreach (var lockedRoomId in lockedRoomIds)
                        {
                            var releaseLockKey = $"{lockedRoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                            _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                        }

                        return new ResultModel
                        {
                            IsSuccess = false,
                            Message = $"Phòng {room.RoomName} đang được đặt bởi người khác. Vui lòng thử lại!",
                            StatusCode = StatusCodes.Status409Conflict
                        };
                    }

                    lockedRoomIds.Add(room.RoomId);
                    selectedRooms.Add(room);
                }

                roomTypeDetails.Add(new RoomTypeQuantityDto
                {
                    RoomTypeId = roomTypeId,
                    RoomTypeName = roomType.TypeName,
                    Quantity = quantity,
                    PricePerNight = roomType.BasePriceNight
                });
            }

            // 6. Calculate total amount and prepare booking rooms
            decimal totalAmount = 0;
            var numberOfNights = (request.CheckOutDate - request.CheckInDate).Days;
            var bookingRoomsToAdd = new List<BookingRoom>();

            foreach (var room in selectedRooms)
            {
                // Lấy RoomType để lấy BasePriceNight
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(room.RoomTypeId);
                if (roomType == null) continue;

                // Tính giá có áp dụng điều chỉnh theo ngày lễ
                var roomSubTotal = await CalculateRoomPriceWithHolidayAsync(
                    room.RoomId, 
                    request.CheckInDate, 
                    request.CheckOutDate, 
                    roomType.BasePriceNight
                );
                
                totalAmount += roomSubTotal;

                bookingRoomsToAdd.Add(new BookingRoom
                {
                    RoomId = room.RoomId,
                    PricePerNight = roomType.BasePriceNight,
                    NumberOfNights = numberOfNights,
                    SubTotal = roomSubTotal,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate
                });
            }

            // 7. Create booking
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalAmount = totalAmount,
                DepositAmount = totalAmount * 0.3m, // 30% deposit
                SpecialRequests = request.SpecialRequests,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null
            };

            // Get status codes
            var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
            var unpaidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "DepositStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
            var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && c.CodeName == request.BookingType)).FirstOrDefault();

            if (unpaidStatus != null) booking.PaymentStatusId = unpaidStatus.CodeId;
            if (unpaidDepositStatus != null) booking.DepositStatusId = unpaidDepositStatus.CodeId;
            if (bookingTypeCode != null) booking.BookingTypeId = bookingTypeCode.CodeId;

            await _unitOfWork.Bookings.AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // 8. Create booking rooms
            foreach (var bookingRoom in bookingRoomsToAdd)
            {
                bookingRoom.BookingId = booking.BookingId;
                await _unitOfWork.BookingRooms.AddAsync(bookingRoom);
            }
            await _unitOfWork.SaveChangesAsync();

            // 9. Create PayOS payment link
            string paymentUrl = string.Empty;
            long orderCode = 0;
            try
            {
                orderCode = long.Parse(DateTimeOffset.Now.ToString("yyMMddHHmmss"));
                var returnUrl = _configuration["PayOS:ReturnUrl"] ?? "http://localhost:5173/payment/callback";
                var cancelUrl = _configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel";

                var roomNames = string.Join(", ", selectedRooms.Select(r => r.RoomName));

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)booking.DepositAmount,
                    description: $"Dat coc phong {roomNames} - {request.FullName}",
                    items: new List<ItemData>
                    {
                        new ItemData($"Booking #{booking.BookingId}", 1, (int)booking.DepositAmount)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl
                );

                var createPayment = await _payOS.createPaymentLink(paymentData);
                paymentUrl = createPayment.checkoutUrl;

                // Save payment info to cache
                _cacheHelper.Set(CachePrefix.BookingPayment, booking.BookingId.ToString(), new
                {
                    booking.BookingId,
                    OrderCode = orderCode,
                    Amount = booking.DepositAmount,
                    LockId = lockId,
                    RoomIds = lockedRoomIds,
                    request.CheckInDate,
                    request.CheckOutDate,
                    CustomerEmail = request.Email,
                    CustomerPhone = request.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                // Rollback
                var bookingRoomsToDelete = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId);
                foreach (var bookingRoom in bookingRoomsToDelete)
                {
                    await _unitOfWork.BookingRooms.DeleteAsync(bookingRoom);
                }
                
                await _unitOfWork.Bookings.DeleteAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                // Release room locks
                foreach (var roomId in lockedRoomIds)
                {
                    var releaseLockKey = $"{roomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
                }

                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi tạo link thanh toán: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // 10. Create Transaction record
            var paymentMethodCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentMethod" && c.CodeName == "EWallet")).FirstOrDefault();
            var transactionStatusCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "Status" && c.CodeName == "Pending")).FirstOrDefault();

            if (paymentMethodCode != null && transactionStatusCode != null && unpaidStatus != null)
            {
                var transaction = new BusinessTransaction
                {
                    BookingId = booking.BookingId,
                    TotalAmount = booking.TotalAmount,
                    PaidAmount = 0,
                    DepositAmount = booking.DepositAmount,
                    PaymentMethodId = paymentMethodCode.CodeId,
                    PaymentStatusId = unpaidStatus.CodeId,
                    TransactionStatusId = transactionStatusCode.CodeId,
                    OrderCode = orderCode.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };
                await _unitOfWork.Transactions.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();
            }

            // 11. Enqueue message to process booking
            var message = new BookingQueueMessage
            {
                MessageType = BookingMessageType.CreateBooking,
                Data = new BookingMessage
                {
                    BookingId = booking.BookingId,
                    CustomerId = customer.CustomerId,
                    RoomIds = lockedRoomIds,
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalAmount = totalAmount,
                    BookingType = request.BookingType,
                    LockId = lockId
                }
            };

            await _queueService.EnqueueAsync(message);

            // 12. Schedule auto-cancel after 15 minutes if not paid
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(15));
                var paymentInfo = _cacheHelper.Get<object>(CachePrefix.BookingPayment, booking.BookingId.ToString());
                if (paymentInfo != null)
                {
                    // Payment not completed, cancel booking
                    var cancelMessage = new BookingQueueMessage
                    {
                        MessageType = BookingMessageType.CancelBooking,
                        Data = new BookingMessage
                        {
                            BookingId = booking.BookingId,
                            RoomIds = lockedRoomIds,
                            CheckInDate = request.CheckInDate,
                            CheckOutDate = request.CheckOutDate,
                            LockId = lockId
                        }
                    };
                    await _queueService.EnqueueAsync(cancelMessage);
                }
            });

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Tạo booking thành công. Vui lòng thanh toán trong 15 phút!",
                Data = new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.FullName,
                    RoomIds = lockedRoomIds,
                    RoomNames = selectedRooms.Select(r => r.RoomName).ToList(),
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    TotalAmount = totalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentUrl = paymentUrl,
                    CreatedAt = booking.CreatedAt
                },
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> GetBookingByIdAsync(int bookingId)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Booking không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
            var rooms = await Task.WhenAll(roomIds.Select(id => _unitOfWork.Rooms.GetByIdAsync(id)));
            var roomNames = rooms.Where(r => r != null).Select(r => r!.RoomName).ToList();

            var paymentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId ?? 0);
            var depositStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.DepositStatusId ?? 0);
            var bookingType = await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId ?? 0);

            return new ResultModel
            {
                IsSuccess = true,
                Data = new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = booking.CustomerId,
                    CustomerName = customer?.FullName ?? "",
                    RoomIds = roomIds,
                    RoomNames = roomNames,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentStatus = paymentStatus?.CodeValue ?? "",
                    DepositStatus = depositStatus?.CodeValue ?? "",
                    BookingType = bookingType?.CodeValue ?? "",
                    SpecialRequests = booking.SpecialRequests,
                    CreatedAt = booking.CreatedAt
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> ConfirmPaymentAsync(ConfirmPaymentRequest request)
        {
            // Get payment info from cache
            var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, request.BookingId.ToString());
            if (paymentInfo == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Thông tin thanh toán không tồn tại hoặc đã hết hạn",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Verify payment with PayOS
            try
            {
                var paymentLinkInfo = await _payOS.getPaymentLinkInformation(long.Parse(request.OrderCode));
                
                if (paymentLinkInfo.status != "PAID")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Thanh toán chưa hoàn tất",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi xác thực thanh toán: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // Update booking status
            var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
            if (booking == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Booking không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var paidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "DepositStatus" && c.CodeName == "Paid")).FirstOrDefault();

            if (paidDepositStatus != null)
            {
                booking.DepositStatusId = paidDepositStatus.CodeId;
            }

            booking.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Bookings.UpdateAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // Enqueue message to confirm payment and release locks
            var message = new BookingQueueMessage
            {
                MessageType = BookingMessageType.ConfirmPayment,
                Data = new BookingMessage
                {
                    BookingId = request.BookingId,
                    LockId = paymentInfo.LockId,
                    RoomIds = paymentInfo.RoomIds,
                    CheckInDate = paymentInfo.CheckInDate,
                    CheckOutDate = paymentInfo.CheckOutDate
                }
            };

            await _queueService.EnqueueAsync(message);

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Xác nhận thanh toán thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> CancelBookingAsync(int bookingId, int userId)
        {
            var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
            if (booking == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Booking không tồn tại",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Get payment info from cache
            var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString());
            
            // Get booking rooms
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            var roomIds = bookingRooms.Select(br => br.RoomId).ToList();

            // Enqueue cancel message
            var message = new BookingQueueMessage
            {
                MessageType = BookingMessageType.CancelBooking,
                Data = new BookingMessage
                {
                    BookingId = bookingId,
                    RoomIds = roomIds,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    LockId = paymentInfo?.LockId ?? ""
                }
            };

            await _queueService.EnqueueAsync(message);

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Hủy booking thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetMyBookingsAsync(int customerId)
        {
            var bookings = await _unitOfWork.Bookings.FindAsync(b => b.CustomerId == customerId);
            var bookingDtos = new List<BookingDto>();

            foreach (var booking in bookings.OrderByDescending(b => b.CreatedAt))
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(booking.CustomerId);
                var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == booking.BookingId);
                var roomIds = bookingRooms.Select(br => br.RoomId).ToList();
                var rooms = await Task.WhenAll(roomIds.Select(id => _unitOfWork.Rooms.GetByIdAsync(id)));
                var roomNames = rooms.Where(r => r != null).Select(r => r!.RoomName).ToList();

                var paymentStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.PaymentStatusId ?? 0);
                var depositStatus = await _unitOfWork.CommonCodes.GetByIdAsync(booking.DepositStatusId ?? 0);
                var bookingType = await _unitOfWork.CommonCodes.GetByIdAsync(booking.BookingTypeId ?? 0);

                bookingDtos.Add(new BookingDto
                {
                    BookingId = booking.BookingId,
                    CustomerId = booking.CustomerId,
                    CustomerName = customer?.FullName ?? "",
                    RoomIds = roomIds,
                    RoomNames = roomNames,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    TotalAmount = booking.TotalAmount,
                    DepositAmount = booking.DepositAmount,
                    PaymentStatus = paymentStatus?.CodeValue ?? "",
                    DepositStatus = depositStatus?.CodeValue ?? "",
                    BookingType = bookingType?.CodeValue ?? "",
                    SpecialRequests = booking.SpecialRequests,
                    CreatedAt = booking.CreatedAt
                });
            }

            return new ResultModel
            {
                IsSuccess = true,
                Data = bookingDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetMyBookingsByAccountIdAsync(int accountId)
        {
            // First, get customer by accountId
            var customers = await _unitOfWork.Customers.FindAsync(c => c.AccountId == accountId);
            var customer = customers.FirstOrDefault();
            
            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy thông tin khách hàng",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }
            
            // Use existing GetMyBookingsAsync method
            return await GetMyBookingsAsync(customer.CustomerId);
        }
    }
}
