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
        private List<int>? _occupiedStatusIdsCache;

        public BookingHelperService(IUnitOfWork unitOfWork, CacheHelper cacheHelper, ILogger<BookingHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheHelper = cacheHelper;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách status code Id đang chiếm phòng (cache theo scope để tránh query lặp)
        /// </summary>
        private async Task<List<int>> GetOccupiedStatusIdsAsync()
        {
            if (_occupiedStatusIdsCache != null && _occupiedStatusIdsCache.Any())
            {
                return _occupiedStatusIdsCache;
            }

            var occupiedStatuses = await _unitOfWork.CommonCodes.FindAsync(c =>
                c.CodeType == "BookingStatus" &&
                (c.CodeName == "PendingConfirmation" || c.CodeName == "Confirmed" || c.CodeName == "CheckedIn"));

            _occupiedStatusIdsCache = occupiedStatuses.Select(s => s.CodeId).ToList();
            _logger.LogInformation("[GetOccupiedStatusIds] Loaded occupied status ids: {Ids}", string.Join(",", _occupiedStatusIdsCache));
            return _occupiedStatusIdsCache;
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
                var occupiedStatusIds = await GetOccupiedStatusIdsAsync();
                
                _logger.LogInformation("[IsRoomAvailableAsync] Found {Count} occupied statuses: {StatusIds}", 
                    occupiedStatusIds.Count, string.Join(",", occupiedStatusIds));

                if (!occupiedStatusIds.Any())
                {
                    _logger.LogWarning("[IsRoomAvailableAsync] No occupied status codes found, allowing booking");
                    return true;
                }

                // Kiểm tra booking trong database với date range overlap + status chiếm phòng
                var overlappingBookingRooms = await _unitOfWork.BookingRooms.FindAsync(
                    br =>
                        br.RoomId == roomId &&
                        (ignoreBookingId == null || br.BookingId != ignoreBookingId.Value) &&
                        br.Booking.StatusId.HasValue &&
                        occupiedStatusIds.Contains(br.Booking.StatusId.Value) &&
                        br.Booking.CheckInDate < checkOutDate &&
                        br.Booking.CheckOutDate > checkInDate,
                    br => br.Booking);

                var overlappingList = overlappingBookingRooms.ToList();

                _logger.LogInformation("[IsRoomAvailableAsync] Found {Count} overlapping occupied bookings for room {RoomId}", 
                    overlappingList.Count, roomId);

                if (!overlappingList.Any())
                {
                    _logger.LogInformation("[IsRoomAvailableAsync] Room {RoomId} is AVAILABLE (no occupied bookings found)", roomId);
                    return true;
                }

                foreach (var bookingRoom in overlappingList)
                {
                    var booking = bookingRoom.Booking;
                    if (booking == null)
                    {
                        _logger.LogWarning("[IsRoomAvailableAsync] Booking {BookingId} data not loaded for Room {RoomId}", bookingRoom.BookingId, roomId);
                        continue;
                    }
                    _logger.LogWarning("[IsRoomAvailableAsync] Room {RoomId} blocked by Booking {BookingId} (StatusId={StatusId}, {CheckIn} -> {CheckOut})",
                        roomId,
                        bookingRoom.BookingId,
                        booking.StatusId,
                        booking.CheckInDate,
                        booking.CheckOutDate);
                }

                return false;
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
        /// Lấy toàn bộ phòng available theo loại (không dừng khi đủ số lượng)
        /// Dùng cho UI để hiển thị danh sách phòng lựa chọn.
        /// </summary>
        public async Task<List<Room>> FindAllAvailableRoomsByTypeAsync(
            int roomTypeId,
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            _logger.LogInformation("[FindAllAvailableRoomsByTypeAsync] Listing all available rooms of type {RoomTypeId} from {CheckIn} to {CheckOut}",
                roomTypeId, checkInDate, checkOutDate);

            var allRoomsOfType = await _unitOfWork.Rooms.FindAsync(r => r.RoomTypeId == roomTypeId);
            var availableRooms = new List<Room>();

            foreach (var room in allRoomsOfType)
            {
                var isAvailable = await IsRoomAvailableAsync(room.RoomId, checkInDate, checkOutDate);
                if (isAvailable)
                {
                    availableRooms.Add(room);
                    _logger.LogInformation("[FindAllAvailableRoomsByTypeAsync] Room {RoomId} ({RoomName}) is available", room.RoomId, room.RoomName);
                }
                else
                {
                    _logger.LogInformation("[FindAllAvailableRoomsByTypeAsync] Room {RoomId} ({RoomName}) is NOT available", room.RoomId, room.RoomName);
                }
            }

            _logger.LogInformation("[FindAllAvailableRoomsByTypeAsync] Total available rooms found: {Count}", availableRooms.Count);
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
