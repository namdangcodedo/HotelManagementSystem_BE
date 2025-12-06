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
using AppBackend.Services.Services.Email;
using AppBackend.BusinessObjects.Enums;

namespace AppBackend.Services.Services.BookingServices
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly IBookingQueueService _queueService;
        private readonly PayOS _payOS;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly AccountHelper _accountHelper;
        private readonly BookingTokenHelper _bookingTokenHelper;

        public BookingService(
            IUnitOfWork unitOfWork,
            CacheHelper cacheHelper,
            IBookingQueueService queueService,
            PayOS payOS,
            IConfiguration configuration,
            IEmailService emailService,
            AccountHelper accountHelper,
            BookingTokenHelper bookingTokenHelper)
        {
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _queueService = queueService;
            _payOS = payOS;
            _configuration = configuration;
            _emailService = emailService;
            _accountHelper = accountHelper;
            _bookingTokenHelper = bookingTokenHelper;
        }

        public async Task<ResultModel> CheckRoomAvailabilityAsync(CheckRoomAvailabilityRequest request)
        {
            var availabilityResults = new List<RoomTypeAvailabilityDto>();
            var totalNights = (request.CheckOutDate - request.CheckInDate).Days;

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                int roomTypeId = roomTypeRequest.RoomTypeId;
                int requestedQuantity = roomTypeRequest.Quantity;

                // Lấy thông tin RoomType
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    availabilityResults.Add(new RoomTypeAvailabilityDto
                    {
                        RoomTypeId = roomTypeId,
                        RoomTypeName = "Unknown",
                        RequestedQuantity = requestedQuantity,
                        AvailableCount = 0,
                        IsAvailable = false,
                        Message = $"Loại phòng ID {roomTypeId} không tồn tại trong hệ thống"
                    });
                    continue;
                }

                // Lấy tất cả phòng thuộc loại này
                var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
                var availableRooms = new List<Room>();

                // ✅ REFACTORED: Sử dụng hàm chung IsRoomAvailableAsync
                foreach (var room in allRoomsOfType)
                {
                    if (await IsRoomAvailableAsync(room.RoomId, request.CheckInDate, request.CheckOutDate))
                    {
                        availableRooms.Add(room);
                    }
                }

                // Lấy hình ảnh của room type
                var media = await _unitOfWork.Mediums.FindAsync(m => 
                    m.ReferenceTable == "RoomType" && 
                    m.ReferenceKey == roomTypeId.ToString() &&
                    m.IsActive);
                var images = media.OrderBy(m => m.DisplayOrder).Select(m => m.FilePath).ToList();

                // Tạo message phù hợp
                string message;
                if (availableRooms.Count >= requestedQuantity)
                {
                    message = $"✓ Còn {availableRooms.Count} phòng trống, đủ để đáp ứng yêu cầu {requestedQuantity} phòng";
                }
                else if (availableRooms.Count > 0)
                {
                    message = $"⚠ Chỉ còn {availableRooms.Count} phòng trống, không đủ yêu cầu {requestedQuantity} phòng";
                }
                else
                {
                    message = $"✗ Không còn phòng trống trong khoảng thời gian này";
                }

                availabilityResults.Add(new RoomTypeAvailabilityDto
                {
                    RoomTypeId = roomTypeId,
                    RoomTypeName = roomType.TypeName,
                    RoomTypeCode = roomType.TypeCode,
                    Description = roomType.Description ?? "",
                    BasePriceNight = roomType.BasePriceNight,
                    MaxOccupancy = roomType.MaxOccupancy,
                    RoomSize = roomType.RoomSize ?? 0,
                    NumberOfBeds = roomType.NumberOfBeds ?? 0,
                    BedType = roomType.BedType ?? "",
                    AvailableCount = availableRooms.Count,
                    RequestedQuantity = requestedQuantity,
                    IsAvailable = availableRooms.Count >= requestedQuantity,
                    Message = message,
                    Images = images
                });
            }

            // Kiểm tra xem tất cả loại phòng đều đủ số lượng không
            var unavailableTypes = availabilityResults.Where(r => !r.IsAvailable).ToList();
            bool isAllAvailable = !unavailableTypes.Any();
            
            string overallMessage;
            if (isAllAvailable)
            {
                overallMessage = "✓ Tất cả loại phòng đều có đủ số lượng cho khoảng thời gian này";
            }
            else
            {
                var unavailableNames = string.Join(", ", unavailableTypes.Select(t => t.RoomTypeName));
                overallMessage = $"⚠ Một số loại phòng không đủ số lượng: {unavailableNames}";
            }

            var response = new CheckAvailabilityResponse
            {
                IsAllAvailable = isAllAvailable,
                Message = overallMessage,
                RoomTypes = availabilityResults,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                TotalNights = totalNights
            };

            return new ResultModel
            {
                IsSuccess = true,
                Message = overallMessage,
                Data = response,
                StatusCode = isAllAvailable ? StatusCodes.Status200OK : StatusCodes.Status409Conflict
            };
        }

        /// <summary>
        /// Kiểm tra phòng có available không (dùng chung cho tất cả flows)
        /// Logic: Phòng available khi:
        /// 1. Không bị lock trong cache
        /// 2. Không có booking với transaction status = "Completed"
        /// </summary>
        private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkInDate, DateTime checkOutDate, string? ignoreLockId = null)
        {
            // 1. Kiểm tra cache lock
            var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
            var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);

            // Nếu phòng bị lock và không phải lock của mình thì không available
            if (!string.IsNullOrEmpty(lockedBy) && lockedBy != ignoreLockId)
            {
                return false;
            }

            // 2. Kiểm tra booking trong database
            var existingBookings = (await _unitOfWork.BookingRooms.FindAsync(br =>
                br.RoomId == roomId &&
                br.Booking.CheckInDate < checkOutDate &&
                br.Booking.CheckOutDate > checkInDate)).ToList();

            if (!existingBookings.Any())
            {
                return true; // Không có booking nào
            }

            // 3. Kiểm tra xem có booking nào đã thanh toán thành công chưa
            var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

            if (completedStatus == null)
            {
                return true; // Không tìm thấy status code, cho phép đặt
            }

            foreach (var bookingRoom in existingBookings)
            {
                var transactions = await _unitOfWork.Transactions.FindAsync(t => 
                    t.BookingId == bookingRoom.BookingId);

                // Nếu có bất kỳ transaction nào đã Completed, phòng đã được đặt
                if (transactions.Any(t => t.TransactionStatusId == completedStatus.CodeId))
                {
                    return false;
                }
            }

            return true; // Không có booking nào đã thanh toán thành công
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

                // Sử dụng hàm chung để kiểm tra availability
                if (await IsRoomAvailableAsync(room.RoomId, checkInDate, checkOutDate))
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
            numberOfNights = numberOfNights == 0 ? 1 : numberOfNights;

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

        /// <summary>
        /// Tìm BookingId từ OrderCode
        /// </summary>
        private async Task<int> FindBookingIdByOrderCodeAsync(long orderCode)
        {
            var transaction = (await _unitOfWork.Transactions.FindAsync(t => 
                t.OrderCode == orderCode.ToString())).FirstOrDefault();
            
            return transaction?.BookingId ?? 0;
        }

        public async Task<ResultModel> CreateBookingAsync(CreateBookingRequest request, int userId)
        {
            // 1. Validate RoomTypes
            if (request.RoomTypes == null || !request.RoomTypes.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Vui lòng chọn ít nhất một loại phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 2. Get customer by userId (from token)
            var customers = await _unitOfWork.Customers.FindAsync(c => c.AccountId == userId);
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

            // 3. Generate unique lock ID
            var lockId = Guid.NewGuid().ToString();

            // 4. Tìm phòng available cho từng loại phòng và lock chúng với cache handling
            var selectedRooms = new List<Room>();
            var lockedRoomIds = new List<int>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                int roomTypeId = roomTypeRequest.RoomTypeId;
                int quantity = roomTypeRequest.Quantity;

                // Lấy thông tin RoomType
                var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(roomTypeId);
                if (roomType == null)
                {
                    // Release all locked rooms
                    _cacheHelper.ReleaseAllBookingLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);

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
                    _cacheHelper.ReleaseAllBookingLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);

                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = $"Không đủ phòng {roomType.TypeName}. Yêu cầu: {quantity}, Còn trống: {availableRooms.Count}",
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }

                // Lock các phòng đã chọn với cache handling để tránh tranh chấp
                foreach (var room in availableRooms.Take(quantity))
                {
                    var lockKey = $"{room.RoomId}_{request.CheckInDate:yyyyMMdd}_{request.CheckOutDate:yyyyMMdd}";
                    var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

                    if (!locked)
                    {
                        // Release all previously locked rooms
                        _cacheHelper.ReleaseAllBookingLocks(lockedRoomIds, request.CheckInDate, request.CheckOutDate, lockId);

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
                    RoomTypeCode = roomType.TypeCode,
                    Quantity = quantity,
                    PricePerNight = roomType.BasePriceNight
                });
            }

            // 5. Calculate total amount and prepare booking rooms
            decimal totalAmount = 0;
            var numberOfNights = (request.CheckOutDate - request.CheckInDate).Days;
            numberOfNights = numberOfNights == 0 ? 1 : numberOfNights;
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
                CustomerId = userId,
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
            
            // BookingType luôn là "Online" cho web booking - tìm với ignore case
            var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && 
                c.CodeName.ToLower() == "online")).FirstOrDefault();

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
                var cancelUrl = (_configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel") + $"?bookingId={booking.BookingId}";

                var roomNames = string.Join(", ", selectedRooms.Select(r => r.RoomName));
                
                // PayOS giới hạn description tối đa 25 ký tự
                var description = $"Dat phong #{booking.BookingId}";
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                // Set expiration time to 30 minutes from now (use UTC to avoid timezone issues)
                var expiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)booking.DepositAmount,
                    description: description,
                    items: new List<ItemData>
                    {
                        new ItemData($"Booking #{booking.BookingId}", 1, (int)booking.DepositAmount)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl,
                    expiredAt: expiredAt
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
                    LockId = lockId
                }
            };

            await _queueService.EnqueueAsync(message);

            // 11. Schedule auto-cancel after 15 minutes if not paid
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));//test 1 minute
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

            // Validate RoomTypes
            if (!request.RoomTypes.Any())
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Vui lòng chọn ít nhất một loại phòng",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 3. Tạo Account và Customer mới cho Guest
            Customer? customer = null;
            string? newAccountPassword = null;
            
            // Kiểm tra xem email đã tồn tại trong hệ thống chưa
            var existingAccount = await _unitOfWork.Accounts.GetByEmailAsync(request.Email);
            
            if (existingAccount != null)
            {
                // Nếu account đã tồn tại, lấy customer liên kết
                var existingCustomers = await _unitOfWork.Customers.FindAsync(c => c.AccountId == existingAccount.AccountId);
                customer = existingCustomers.FirstOrDefault();
                
                if (customer != null)
                {
                    // Cập nhật thông tin customer nếu cần
                    bool needUpdate = false;
                    
                    if (!string.IsNullOrEmpty(request.FullName) && customer.FullName != request.FullName)
                    {
                        customer.FullName = request.FullName;
                        needUpdate = true;
                    }
                    
                    if (!string.IsNullOrEmpty(request.PhoneNumber) && customer.PhoneNumber != request.PhoneNumber)
                    {
                        customer.PhoneNumber = request.PhoneNumber;
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
            }
            else
            {
                // Tạo Account mới với mật khẩu mặc định "123456"
                newAccountPassword = "123456";
                var hashedPassword = _accountHelper.HashPassword(newAccountPassword);
                
                var newAccount = new Account
                {
                    Username = request.Email, // Sử dụng email làm username
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = null
                };
                
                await _unitOfWork.Accounts.AddAsync(newAccount);
                await _unitOfWork.SaveChangesAsync();
                
                // Gán role User cho account mới
                var userRole = await _unitOfWork.Roles.GetRoleByRoleValueAsync(RoleEnums.User.ToString());
                if (userRole != null)
                {
                    var accountRole = new AccountRole
                    {
                        AccountId = newAccount.AccountId,
                        RoleId = userRole.RoleId
                    };
                    await _unitOfWork.Accounts.AddAccountRoleAsync(accountRole);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                // Tạo Customer mới và liên kết với Account
                customer = new Customer
                {
                    AccountId = newAccount.AccountId,
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
            
            if (customer == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = "Không thể tạo thông tin khách hàng",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            // 4. Generate unique lock ID
            var lockId = Guid.NewGuid().ToString();

            // 5. Tìm phòng available cho từng loại phòng và lock chúng
            var selectedRooms = new List<Room>();
            var lockedRoomIds = new List<int>();
            var roomTypeDetails = new List<RoomTypeQuantityDto>();

            foreach (var roomTypeRequest in request.RoomTypes)
            {
                int roomTypeId = roomTypeRequest.RoomTypeId;
                int quantity = roomTypeRequest.Quantity;

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

                // Lock các phòng đã chọn với cache handling để tránh tranh chấp
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
                    RoomTypeCode = roomType.TypeCode,
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
            
            // BookingType luôn là "Online" cho web booking - tìm với ignore case
            var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingType" && 
                c.CodeName.ToLower() == "online")).FirstOrDefault();

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
                var cancelUrl = (_configuration["PayOS:CancelUrl"] ?? "http://localhost:5173/payment/cancel") + $"?bookingId={booking.BookingId}";

                var roomNames = string.Join(", ", selectedRooms.Select(r => r.RoomName));
                
                // PayOS giới hạn description tối đa 25 ký tự
                var description = $"Dat phong #{booking.BookingId}";
                if (description.Length > 25)
                {
                    description = description.Substring(0, 25);
                }

                // Set expiration time to 30 minutes from now (use UTC to avoid timezone issues)
                var expiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

                var paymentData = new PaymentData(
                    orderCode: orderCode,
                    amount: (int)booking.DepositAmount,
                    description: description,
                    items: new List<ItemData>
                    {
                        new ItemData($"Booking #{booking.BookingId}", 1, (int)booking.DepositAmount)
                    },
                    cancelUrl: cancelUrl,
                    returnUrl: returnUrl,
                    expiredAt: expiredAt
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
                    CreatedBy = null
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
                    LockId = lockId
                }
            };

            await _queueService.EnqueueAsync(message);

            // 11. Schedule auto-cancel after 15 minutes if not paid
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));//test 1 minute
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

        public async Task<ResultModel> GetBookingByTokenAsync(string token)
        {
            try
            {
                // Decode token to get bookingId
                var bookingId = _bookingTokenHelper.DecodeBookingToken(token);
                
                // Use existing method to get booking details
                return await GetBookingByIdAsync(bookingId);
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Token không hợp lệ: {ex.Message}",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
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

            // ✅ FIX: Update trạng thái Transaction thành "Cancelled"
            var cancelledTransactionStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "TransactionStatus" && c.CodeName == "Cancelled")).FirstOrDefault();
            
            if (cancelledTransactionStatus != null)
            {
                // Update tất cả transactions của booking này thành Cancelled
                var transactions = await _unitOfWork.Transactions.FindAsync(t => t.BookingId == bookingId);
                foreach (var transaction in transactions)
                {
                    transaction.TransactionStatusId = cancelledTransactionStatus.CodeId;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    transaction.UpdatedBy = userId;
                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                }
                await _unitOfWork.SaveChangesAsync();
            }

            // ✅ FIX: Update trạng thái Booking PaymentStatus thành "Cancelled"
            var cancelledPaymentStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "PaymentStatus" && c.CodeName == "Cancelled")).FirstOrDefault();
            
            if (cancelledPaymentStatus != null)
            {
                booking.PaymentStatusId = cancelledPaymentStatus.CodeId;
                booking.UpdatedAt = DateTime.UtcNow;
                booking.UpdatedBy = userId;
                await _unitOfWork.Bookings.UpdateAsync(booking);
                await _unitOfWork.SaveChangesAsync();
            }

            // Get payment info from cache
            var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString());
            
            // Get booking rooms
            var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
            var roomIds = bookingRooms.Select(br => br.RoomId).ToList();

            // Release room locks nếu có
            if (paymentInfo != null && !string.IsNullOrEmpty(paymentInfo.LockId?.ToString()))
            {
                _cacheHelper.ReleaseAllBookingLocks(roomIds, booking.CheckInDate, booking.CheckOutDate, paymentInfo.LockId.ToString());
            }
            
            // Remove payment info from cache
            _cacheHelper.Remove(CachePrefix.BookingPayment, bookingId.ToString());

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
                    LockId = paymentInfo?.LockId?.ToString() ?? ""
                }
            };

            await _queueService.EnqueueAsync(message);

            return new ResultModel
            {
                IsSuccess = true,
                Message = "Hủy booking thành công. Transaction và Booking đã được cập nhật trạng thái 'Cancelled'",
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

        /// <summary>
        /// Webhook handler - Xác nhận thanh toán và gửi email
        /// </summary>
        public async Task<ResultModel> HandlePayOSWebhookAsync(PayOSWebhookRequest request)
        {
            try
            {
                // 1. Verify webhook từ PayOS SDK - sử dụng WebhookType
                WebhookType webhookBody = new WebhookType(
                    code: request.Code,
                    desc: request.Desc,
                    data: new WebhookData(
                        orderCode: request.Data.OrderCode,
                        amount: request.Data.Amount,
                        description: request.Data.Description ?? "",
                        accountNumber: request.Data.AccountNumber ?? "",
                        reference: request.Data.Reference ?? "",
                        transactionDateTime: request.Data.TransactionDateTime ?? "",
                        currency: request.Data.Currency ?? "VND",
                        paymentLinkId: request.Data.PaymentLinkId ?? "",
                        code: request.Data.Code ?? "00",
                        desc: request.Data.Desc ?? "",
                        counterAccountBankId: request.Data.CounterAccountBankId,
                        counterAccountBankName: request.Data.CounterAccountBankName,
                        counterAccountName: request.Data.CounterAccountName,
                        counterAccountNumber: request.Data.CounterAccountNumber,
                        virtualAccountName: request.Data.VirtualAccountName,
                        virtualAccountNumber: request.Data.VirtualAccountNumber
                    ),
                    signature: request.Signature,
                    success: request.Success
                );

                var webhookData = _payOS.verifyPaymentWebhookData(webhookBody);
                
                if (webhookData == null || webhookData.code != "00")
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Thanh toán không thành công hoặc webhook không hợp lệ",
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                // 2. Tìm BookingId từ OrderCode
                long orderCode = webhookData.orderCode;
                var bookingId = await FindBookingIdByOrderCodeAsync(orderCode);

                if (bookingId == 0)
                {
                    bookingId = _cacheHelper.GetCustom<int>($"order_{orderCode}");
                }

                if (bookingId == 0)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy booking từ orderCode",
                        StatusCode = StatusCodes.Status200OK
                    };
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Booking không tồn tại",
                        StatusCode = StatusCodes.Status200OK
                    };
                }

                var paidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "PaymentStatus" && c.CodeName == "Paid")).FirstOrDefault();
                var paidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "DepositStatus" && c.CodeName == "Paid")).FirstOrDefault();

                if (paidDepositStatus != null)
                {
                    booking.DepositStatusId = paidDepositStatus.CodeId;
                    booking.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Bookings.UpdateAsync(booking);
                    await _unitOfWork.SaveChangesAsync();
                }

                var transaction = (await _unitOfWork.Transactions.FindAsync(t => 
                    t.BookingId == bookingId && t.OrderCode == orderCode.ToString())).FirstOrDefault();

                if (transaction != null && paidStatus != null)
                {
                    transaction.PaidAmount = webhookData.amount;
                    transaction.PaymentStatusId = paidStatus.CodeId;
                    transaction.TransactionRef = webhookData.reference;
                    transaction.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Transactions.UpdateAsync(transaction);
                    await _unitOfWork.SaveChangesAsync();
                }

                var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString());
                string? newAccountPassword = paymentInfo?.NewAccountPassword;

                try
                {
                    await _emailService.SendBookingConfirmationEmailAsync(bookingId, newAccountPassword);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Failed to send confirmation email: {emailEx.Message}");
                }

                if (paymentInfo != null)
                {
                    var message = new BookingQueueMessage
                    {
                        MessageType = BookingMessageType.ConfirmPayment,
                        Data = new BookingMessage
                        {
                            BookingId = bookingId,
                            LockId = paymentInfo.LockId,
                            RoomIds = paymentInfo.RoomIds,
                            CheckInDate = paymentInfo.CheckInDate,
                            CheckOutDate = paymentInfo.CheckOutDate
                        }
                    };

                    await _queueService.EnqueueAsync(message);

                    _cacheHelper.Remove(CachePrefix.BookingPayment, bookingId.ToString());
                }

                _cacheHelper.RemoveCustom($"order_{orderCode}");

                return new ResultModel
                {
                    IsSuccess = true,
                    Message = "Xác nhận thanh toán thành công",
                    Data = new { BookingId = bookingId },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi xử lý webhook: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Hủy cache booking và release room locks khi user hủy thanh toán
        /// FE gọi API này khi user click "Cancel" hoặc thoát khỏi trang thanh toán
        /// </summary>
        public async Task<ResultModel> CancelBookingCacheAsync(int bookingId)
        {
            try
            {
                // 1. Lấy thông tin booking từ cache
                var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString());
                
                if (paymentInfo == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy thông tin booking trong cache (có thể đã hết hạn hoặc đã thanh toán)",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // 2. Release tất cả room locks
                string lockId = paymentInfo.LockId;
                List<int> roomIds = paymentInfo.RoomIds;
                DateTime checkInDate = paymentInfo.CheckInDate;
                DateTime checkOutDate = paymentInfo.CheckOutDate;

                int releasedCount = 0;
                foreach (var roomId in roomIds)
                {
                    var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
                    var released = _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
                    if (released)
                    {
                        releasedCount++;
                    }
                }

                // 3. Xóa payment info từ cache
                _cacheHelper.Remove(CachePrefix.BookingPayment, bookingId.ToString());

                // 4. Xóa orderCode cache nếu có
                long? orderCode = paymentInfo.OrderCode;
                if (orderCode.HasValue)
                {
                    _cacheHelper.RemoveCustom($"order_{orderCode.Value}");
                }

                // 5. (Optional) Xóa booking record khỏi DB nếu chưa thanh toán
                var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
                if (booking != null)
                {
                    // Kiểm tra xem đã có transaction Completed chưa
                    var completedStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
                        c.CodeType == "TransactionStatus" && c.CodeName == "Completed")).FirstOrDefault();

                    var hasCompletedTransaction = false;
                    if (completedStatus != null)
                    {
                        var transactions = await _unitOfWork.Transactions.FindAsync(t => t.BookingId == bookingId);
                        hasCompletedTransaction = transactions.Any(t => t.TransactionStatusId == completedStatus.CodeId);
                    }

                    // Chỉ xóa nếu chưa thanh toán
                    if (!hasCompletedTransaction)
                    {
                        // Xóa BookingRooms trước
                        var bookingRooms = await _unitOfWork.BookingRooms.FindAsync(br => br.BookingId == bookingId);
                        foreach (var bookingRoom in bookingRooms)
                        {
                            await _unitOfWork.BookingRooms.DeleteAsync(bookingRoom);
                        }

                        // Xóa Transactions
                        var transactions = await _unitOfWork.Transactions.FindAsync(t => t.BookingId == bookingId);
                        foreach (var transaction in transactions)
                        {
                            await _unitOfWork.Transactions.DeleteAsync(transaction);
                        }

                        // Xóa Booking
                        await _unitOfWork.Bookings.DeleteAsync(booking);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                return new ResultModel
                {
                    IsSuccess = true,
                    Message = $"Đã hủy booking và giải phóng {releasedCount}/{roomIds.Count} phòng thành công",
                    Data = new
                    {
                        BookingId = bookingId,
                        ReleasedRooms = releasedCount,
                        TotalRooms = roomIds.Count,
                        RoomIds = roomIds
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    Message = $"Lỗi khi hủy booking cache: {ex.Message}",
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
