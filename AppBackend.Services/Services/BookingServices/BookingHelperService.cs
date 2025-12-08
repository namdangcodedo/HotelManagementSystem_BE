using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.Helpers;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.BookingServices
{
    /// <summary>
    /// Service chung cho các logic booking được dùng bởi cả BookingService và BookingManagementService
    /// Tránh code trùng lặp và dễ maintain
    /// </summary>
    public class BookingHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CacheHelper _cacheHelper;
        private readonly ILogger<BookingHelperService> _logger;

        public BookingHelperService(IUnitOfWork unitOfWork, CacheHelper cacheHelper, ILogger<BookingHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra phòng có available không
        /// Logic: Phòng KHÔNG available nếu có booking với status:
        /// - PendingConfirmation: Khách đã báo chuyển khoản, chờ manager xác nhận
        /// - Confirmed: Manager đã check bill và xác nhận nhận được tiền
        /// - CheckedIn: Khách đang ở
        /// 
        /// Phòng VẪN available nếu booking có status:
        /// - Pending: Chưa thanh toán, sẽ tự động hủy sau 15 phút
        /// - Cancelled: Đã hủy
        /// - Completed: Đã hoàn thành (không overlap về thời gian)
        /// </summary>
        /// <param name="roomId">ID phòng cần kiểm tra</param>
        /// <param name="checkInDate">Ngày check-in</param>
        /// <param name="checkOutDate">Ngày check-out</param>
        /// <param name="ignoreBookingId">Booking ID để bỏ qua (khi update booking)</param>
        public async Task<bool> IsRoomAvailableAsync(
            int roomId, 
            DateTime checkInDate, 
            DateTime checkOutDate, 
            int? ignoreBookingId = null)
        {
            try
            {
                _logger.LogInformation("[IsRoomAvailableAsync] Checking room {RoomId} from {CheckIn} to {CheckOut}", 
                    roomId, checkInDate, checkOutDate);

                // Lấy các status code cần kiểm tra - phòng KHÔNG available với các status này
                var occupiedStatuses = await _unitOfWork.CommonCodes.FindAsync(c =>
                    c.CodeType == "BookingStatus" && 
                    (c.CodeName == "PendingConfirmation" || c.CodeName == "Confirmed" || c.CodeName == "CheckedIn"));

                var occupiedStatusIds = occupiedStatuses.Select(s => s.CodeId).ToList();
                
                _logger.LogInformation("[IsRoomAvailableAsync] Found {Count} occupied statuses: {StatusIds}", 
                    occupiedStatusIds.Count, string.Join(",", occupiedStatusIds));

                if (!occupiedStatusIds.Any())
                {
                    _logger.LogWarning("[IsRoomAvailableAsync] No occupied status codes found, allowing booking");
                    return true;
                }

                // Kiểm tra booking trong database với date range overlap
                var overlappingBookingRooms = await _unitOfWork.BookingRooms.FindAsync(br =>
                    br.RoomId == roomId &&
                    (ignoreBookingId == null || br.BookingId != ignoreBookingId.Value));

                _logger.LogInformation("[IsRoomAvailableAsync] Found {Count} booking rooms for room {RoomId}", 
                    overlappingBookingRooms, roomId);

                if (!overlappingBookingRooms.Any())
                {
                    _logger.LogInformation("[IsRoomAvailableAsync] Room {RoomId} is AVAILABLE (no bookings)", roomId);
                    return true;
                }

                // Kiểm tra xem có booking nào với status occupied và date range overlap không
                foreach (var bookingRoom in overlappingBookingRooms)
                {
                    _logger.LogInformation("[IsRoomAvailableAsync] Checking booking {BookingId} for room {RoomId}", 
                        bookingRoom.BookingId, roomId);

                    if (bookingRoom.Booking == null)
                    {
                        _logger.LogWarning("[IsRoomAvailableAsync] Booking {BookingId} is null, loading from DB", 
                            bookingRoom.BookingId);
                        // Load booking data nếu chưa được load
                        bookingRoom.Booking = await _unitOfWork.Bookings.GetByIdAsync(bookingRoom.BookingId);
                    }

                    if (bookingRoom.Booking == null)
                    {
                        _logger.LogWarning("[IsRoomAvailableAsync] Booking {BookingId} not found in DB, skipping", 
                            bookingRoom.BookingId);
                        continue;
                    }

                    // Kiểm tra date range overlap
                    var hasDateOverlap = bookingRoom.Booking.CheckInDate < checkOutDate && 
                                         bookingRoom.Booking.CheckOutDate > checkInDate;

                    _logger.LogInformation("[IsRoomAvailableAsync] Booking {BookingId}: CheckIn={CheckIn}, CheckOut={CheckOut}, HasDateOverlap={Overlap}, StatusId={StatusId}", 
                        bookingRoom.BookingId, 
                        bookingRoom.Booking.CheckInDate, 
                        bookingRoom.Booking.CheckOutDate,
                        hasDateOverlap,
                        bookingRoom.Booking.StatusId);

                    if (!hasDateOverlap)
                    {
                        _logger.LogInformation("[IsRoomAvailableAsync] Booking {BookingId} has no date overlap, skipping", 
                            bookingRoom.BookingId);
                        continue;
                    }

                    // Nếu booking có StatusId thuộc danh sách occupied statuses
                    if (bookingRoom.Booking.StatusId.HasValue && 
                        occupiedStatusIds.Contains(bookingRoom.Booking.StatusId.Value))
                    {
                        _logger.LogWarning("[IsRoomAvailableAsync] Room {RoomId} is NOT AVAILABLE - Booking {BookingId} has occupied status {StatusId}", 
                            roomId, bookingRoom.BookingId, bookingRoom.Booking.StatusId);
                        return false;
                    }
                }

                _logger.LogInformation("[IsRoomAvailableAsync] Room {RoomId} is AVAILABLE", roomId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IsRoomAvailableAsync] ERROR checking room {RoomId}: {Message}", 
                    roomId, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Tìm và chọn phòng available theo loại phòng
        /// </summary>
        /// <param name="roomTypeId">ID loại phòng</param>
        /// <param name="quantity">Số lượng phòng cần tìm</param>
        /// <param name="checkInDate">Ngày check-in</param>
        /// <param name="checkOutDate">Ngày check-out</param>
        /// <returns>Danh sách phòng available</returns>
        public async Task<List<Room>> FindAvailableRoomsByTypeAsync(
            int roomTypeId, 
            int quantity, 
            DateTime checkInDate, 
            DateTime checkOutDate)
        {
            _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Finding {Quantity} rooms of type {RoomTypeId} from {CheckIn} to {CheckOut}", 
                quantity, roomTypeId, checkInDate, checkOutDate);

            // Lấy tất cả phòng thuộc loại này
            var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            
            _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Found {TotalRooms} rooms of type {RoomTypeId}", 
                allRoomsOfType.Count(), roomTypeId);

            var availableRooms = new List<Room>();

            foreach (var room in allRoomsOfType)
            {
                if (availableRooms.Count >= quantity)
                {
                    _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Found enough rooms ({Count}/{Quantity})", 
                        availableRooms.Count, quantity);
                    break;
                }

                var isAvailable = await IsRoomAvailableAsync(room.RoomId, checkInDate, checkOutDate);
                
                if (isAvailable)
                {
                    availableRooms.Add(room);
                    _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Room {RoomId} ({RoomName}) is available", 
                        room.RoomId, room.RoomName);
                }
                else
                {
                    _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Room {RoomId} ({RoomName}) is NOT available", 
                        room.RoomId, room.RoomName);
                }
            }

            _logger.LogInformation("[FindAvailableRoomsByTypeAsync] Total available rooms found: {Count}/{Quantity}", 
                availableRooms.Count, quantity);

            return availableRooms;
        }

        /// <summary>
        /// Tính số đêm giữa 2 ngày
        /// </summary>
        public int CalculateNumberOfNights(DateTime checkInDate, DateTime checkOutDate)
        {
            return (checkOutDate.Date - checkInDate.Date).Days;
        }

        /// <summary>
        /// Tính giá phòng có áp dụng holiday pricing
        /// </summary>
        public async Task<decimal> CalculateRoomPriceWithHolidayAsync(
            int roomId,
            DateTime checkInDate,
            DateTime checkOutDate,
            decimal basePriceNight)
        {
            var numberOfNights = CalculateNumberOfNights(checkInDate, checkOutDate);
            decimal totalPrice = basePriceNight * numberOfNights;

            // Check holiday pricing
            var holidayPricings = await _unitOfWork.HolidayPricings.FindAsync(hp =>
                hp.RoomId == roomId &&
                hp.IsActive &&
                hp.StartDate <= checkOutDate &&
                hp.EndDate >= checkInDate);

            foreach (var holidayPricing in holidayPricings)
            {
                // Tính số đêm overlap với holiday
                var overlapStart = checkInDate > holidayPricing.StartDate ? checkInDate : holidayPricing.StartDate;
                var overlapEnd = checkOutDate < holidayPricing.EndDate ? checkOutDate : holidayPricing.EndDate;
                var overlapNights = (overlapEnd.Date - overlapStart.Date).Days;

                if (overlapNights > 0)
                {
                    totalPrice += holidayPricing.PriceAdjustment * overlapNights;
                }
            }

            return totalPrice;
        }
    }
}
