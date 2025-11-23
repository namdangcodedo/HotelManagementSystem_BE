using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs quản lý đặt phòng với cache locking và message queue
    /// 
    /// === XỬ LÝ CACHE CHO QUÁ TRÌNH THANH TOÁN ===
    /// 
    /// 1. ROOM BOOKING LOCK (Lock từng phòng cụ thể):
    ///    - Cache Key Format: "room_booking_lock:{RoomId}_{CheckInDate:yyyyMMdd}_{CheckOutDate:yyyyMMdd}"
    ///    - Expiry: 10 phút
    ///    - Mục đích: Ngăn chặn nhiều người cùng đặt 1 phòng trong cùng thời điểm
    ///    - Lock được giải phóng khi: Thanh toán thành công, hủy booking, hoặc timeout
    /// 
    /// 2. BOOKING PAYMENT INFO (Thông tin booking chờ thanh toán):
    ///    - Cache Key Format: "booking_payment:{BookingId}"
    ///    - Expiry: 15 phút
    ///    - Data: { BookingId, OrderCode, Amount, LockId, RoomIds, CheckInDate, CheckOutDate }
    ///    - Mục đích: Lưu thông tin tạm để xác thực thanh toán và tự động hủy nếu không thanh toán
    /// 
    /// 3. ROOM TYPE INVENTORY (Số lượng phòng theo loại đang available):
    ///    - Cache Key Format: "room_type_inventory:{RoomTypeId}_{CheckInDate:yyyyMMdd}_{CheckOutDate:yyyyMMdd}"
    ///    - Expiry: 15 phút
    ///    - Mục đích: Track số lượng phòng available theo loại để tránh overbooking khi nhiều request đồng thời
    ///    - Decrement khi lock phòng, Increment khi release lock
    /// 
    /// === LUỒNG XỬ LÝ TRÁNH TRANH CHẤP ===
    /// 
    /// Scenario 1: Hai người cùng đặt phòng cùng loại cùng lúc
    /// - Request A và B cùng check availability → cả 2 đều thấy còn phòng
    /// - Request A lock phòng trước → Success, giảm inventory trong cache
    /// - Request B cố lock cùng phòng → Fail do đã bị lock, tự động chọn phòng khác
    /// - Request B lock phòng khác → Success nếu còn, Fail nếu hết phòng
    /// 
    /// Scenario 2: Timeout thanh toán
    /// - User tạo booking → Lock phòng trong 10 phút
    /// - Sau 15 phút không thanh toán → Tự động hủy booking, release lock, tăng inventory
    /// 
    /// Scenario 3: Thanh toán thành công
    /// - PayOS callback confirm payment → Release lock, xóa payment info khỏi cache
    /// - Phòng chuyển sang trạng thái booked trong database
    /// 
    /// === BEST PRACTICES ===
    /// - Luôn check cache trước khi query database để tránh race condition
    /// - Release lock ngay khi không cần thiết để tối ưu availability
    /// - Sử dụng LockId (GUID) để đảm bảo chỉ người tạo lock mới release được
    /// - Monitor cache hit rate để tối ưu performance
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : BaseApiController
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Kiểm tra phòng có sẵn để đặt không
        /// </summary>
        /// <remarks>
        /// === CACHE HANDLING ===
        /// - Kiểm tra RoomBookingLock cache để loại bỏ phòng đang bị lock
        /// - Kiểm tra RoomTypeInventory cache để lấy số lượng available realtime
        /// - Response bao gồm: Số lượng available, RequestedQuantity, IsAvailable flag
        /// 
        /// === REQUEST EXAMPLE ===
        /// ```json
        /// {
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 2 },
        ///     { "roomTypeId": 3, "quantity": 1 }
        ///   ],
        ///   "checkInDate": "2025-10-25T14:00:00Z",
        ///   "checkOutDate": "2025-10-27T12:00:00Z"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("check-availability")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAvailability([FromBody] CheckRoomAvailabilityRequest request)
        {
            var result = await _bookingService.CheckRoomAvailabilityAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking mới - Trả về BookingId và PayOS payment URL
        /// </summary>
        /// <remarks>
        /// === LUỒNG XỬ LÝ ===
        /// 1. Validate customer và room types
        /// 2. Generate unique LockId (GUID)
        /// 3. Tìm phòng available cho từng room type (FindAvailableRoomsByTypeAsync)
        /// 4. Lock từng phòng vào cache (TryAcquireLock) - 10 phút
        ///    - Nếu lock fail → Release tất cả locks đã tạo và return conflict
        /// 5. Decrement RoomTypeInventory trong cache
        /// 6. Calculate giá có áp dụng holiday pricing
        /// 7. Tạo Booking record trong database
        /// 8. Tạo BookingRoom records (order detail pattern)
        /// 9. Tạo PayOS payment link
        /// 10. Lưu payment info vào cache - 15 phút
        /// 11. Enqueue message vào queue để xử lý background
        /// 12. Schedule auto-cancel sau 15 phút nếu chưa thanh toán
        /// 
        /// === CACHE OPERATIONS ===
        /// - TryAcquireLock: Lock từng phòng với LockId
        /// - DecrementRoomTypeInventory: Giảm số lượng available
        /// - Set BookingPayment: Lưu info để tracking payment
        /// 
        /// === BOOKING TYPE ===
        /// - Tự động gắn "Online" (không cần client gửi)
        /// - Service tự động tìm CommonCode với codeName="Online" (ignore case)
        /// - Map với: { codeId: 33, codeName: "Online", codeValue: "Đặt trực tuyến" }
        /// 
        /// === REQUEST EXAMPLE ===
        /// ```json
        /// {
        ///   "customerId": 5,
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 2 }
        ///   ],
        ///   "checkInDate": "2025-10-25T14:00:00Z",
        ///   "checkOutDate": "2025-10-27T12:00:00Z",
        ///   "specialRequests": "Late check-in"
        /// }
        /// ```
        /// 
        /// NOTE: Không cần gửi field "bookingType" - hệ thống tự động set = "Online"
        /// 
        /// === RESPONSE EXAMPLE ===
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "message": "Tạo booking thành công. Vui lòng thanh toán trong 15 phút!",
        ///   "data": {
        ///     "bookingId": 123,
        ///     "paymentUrl": "https://pay.payos.vn/web/...",
        ///     "totalAmount": 3000000,
        ///     "depositAmount": 900000
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userId = CurrentUserId;
            var result = await _bookingService.CreateBookingAsync(request, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking cho GUEST (không cần đăng nhập/đăng ký tài khoản)
        /// </summary>
        /// <remarks>
        /// === GUEST BOOKING FLOW ===
        /// 1. Người dùng chỉ cần nhập: Họ tên, Email, SĐT, CMND (optional), Địa chỉ (optional)
        /// 2. Hệ thống tự động tạo/tìm Customer record dựa trên PhoneNumber
        /// 3. Cập nhật thông tin customer nếu có thay đổi
        /// 4. Lock phòng và tạo booking (tương tự CreateBooking)
        /// 5. Generate payment link
        /// 6. Lưu thêm CustomerEmail, CustomerPhone vào cache để tracking
        /// 
        /// === CACHE HANDLING ===
        /// - Giống CreateBooking nhưng lưu thêm contact info trong BookingPayment cache
        /// - Dùng để gửi email xác nhận và tracking guest booking
        /// 
        /// === BOOKING TYPE ===
        /// - Mặc định: "Online" (đặt qua web/app)
        /// - Tự động map với CommonCode: codeName="Online", codeValue="Đặt trực tuyến"
        /// - Guest booking luôn là "Online" vì đặt từ web
        /// 
        /// === ƯU ĐIỂM ===
        /// - Không cần đăng ký tài khoản
        /// - Đặt phòng nhanh chóng
        /// - Tự động lưu lịch sử booking theo phone để lần sau dễ dàng hơn
        /// 
        /// === REQUEST EXAMPLE ===
        /// ```json
        /// {
        ///   "fullName": "Nguyen Van A",
        ///   "email": "nguyenvana@example.com",
        ///   "phoneNumber": "0123456789",
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 1 }
        ///   ],
        ///   "checkInDate": "2025-10-25T14:00:00Z",
        ///   "checkOutDate": "2025-10-27T12:00:00Z",
        ///   "bookingType": "Online"
        /// }
        /// ```
        /// 
        /// NOTE: Có thể bỏ qua field "bookingType", hệ thống sẽ tự động set = "Online"
        /// </remarks>
        [HttpPost("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateGuestBooking([FromBody] CreateGuestBookingRequest request)
        {
            var result = await _bookingService.CreateGuestBookingAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết booking
        /// </summary>
        /// <remarks>
        /// Trả về thông tin đầy đủ về booking bao gồm:
        /// - Thông tin customer
        /// - Danh sách phòng đã đặt
        /// - Tổng tiền, tiền cọc
        /// - Trạng thái thanh toán
        /// </remarks>
        [HttpGet("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin booking bằng token (không cần đăng nhập)
        /// </summary>
        /// <remarks>
        /// API này cho phép guest xem thông tin booking từ link trong email mà không cần đăng nhập.
        /// Token được mã hóa 2 chiều từ bookingId để bảo mật.
        /// 
        /// === USAGE ===
        /// - Guest nhận email với link: http://localhost:3000/mybooking/{token}
        /// - Frontend gọi API này với token từ URL
        /// - API decode token để lấy bookingId và trả về thông tin booking
        /// 
        /// === EXAMPLE ===
        /// GET /api/booking/mybooking/abc123xyz456
        /// 
        /// Response:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "customerName": "Nguyen Van A",
        ///   "checkInDate": "2025-10-25T14:00:00",
        ///   "checkOutDate": "2025-10-27T12:00:00",
        ///   "roomNames": ["Deluxe 101", "Deluxe 102"],
        ///   "totalAmount": 5000000,
        ///   "depositAmount": 1500000,
        ///   "paymentStatus": "Paid"
        /// }
        /// ```
        /// </remarks>
        [HttpGet("mybooking/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookingByToken(string token)
        {
            var result = await _bookingService.GetBookingByTokenAsync(token);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Xác nhận thanh toán từ PayOS callback
        /// </summary>
        /// <remarks>
        /// === LUỒNG XỬ LÝ ===
        /// 1. Lấy payment info từ cache (BookingPayment)
        /// 2. Verify với PayOS xem payment đã PAID chưa
        /// 3. Update booking status trong database
        /// 4. Update transaction record
        /// 5. Release tất cả room locks (ReleaseAllBookingLocks)
        /// 6. Remove payment info khỏi cache
        /// 7. Phòng chính thức được booked
        /// 
        /// === CACHE OPERATIONS ===
        /// - Get BookingPayment: Lấy info để verify
        /// - ReleaseAllBookingLocks: Giải phóng locks của các phòng
        /// - Remove BookingPayment: Xóa khỏi cache sau khi success
        /// 
        /// === NOTES ===
        /// - API này được gọi từ PayOS webhook
        /// - AllowAnonymous vì PayOS gọi từ external
        /// - Cần verify signature từ PayOS trong production
        /// </remarks>
        [HttpPost("confirm-payment")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            var result = await _bookingService.ConfirmPaymentAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking
        /// </summary>
        /// <remarks>
        /// === CACHE HANDLING ===
        /// - Release tất cả room locks
        /// - Increment RoomTypeInventory để trả lại số lượng available
        /// - Remove BookingPayment cache
        /// - Update booking status = Cancelled trong database
        /// </remarks>
        [HttpDelete("{bookingId}")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = CurrentUserId;
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách booking của khách hàng hiện tại
        /// </summary>
        [HttpGet("my-bookings")]
        [Authorize]
        public async Task<IActionResult> GetMyBookings()
        {
            var accountId = CurrentUserId;
            var result = await _bookingService.GetMyBookingsByAccountIdAsync(accountId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// PayOS Webhook - Nhận thông báo thanh toán từ PayOS
        /// </summary>
        /// <remarks>
        /// === WEBHOOK PAYOS ===
        /// Endpoint này nhận webhook từ PayOS khi có giao dịch thanh toán thành công hoặc thất bại.
        /// 
        /// === LUỒNG XỬ LÝ ===
        /// 1. Nhận webhook request từ PayOS với format chuẩn
        /// 2. Lấy orderCode từ webhook data
        /// 3. Tìm booking tương ứng với orderCode
        /// 4. Kiểm tra trạng thái thanh toán (success = true, code = "00")
        /// 5. Update trạng thái booking theo CommonCode:
        ///    - PaymentStatus: Tìm theo CodeType="PaymentStatus" và CodeName="Paid"
        ///    - DepositStatus: Tìm theo CodeType="DepositStatus" và CodeName="Paid"
        /// 6. Tạo transaction record với:
        ///    - TransactionStatus: CodeType="TransactionStatus", CodeName="Completed"
        ///    - PaymentMethod: CodeType="PaymentMethod", CodeName="PayOS"
        /// 7. Release room locks và remove cache
        /// 8. Trả về response cho PayOS
        /// 
        /// === REQUEST FORMAT (từ PayOS) ===
        /// ```json
        /// {
        ///   "code": "00",
        ///   "desc": "success",
        ///   "success": true,
        ///   "data": {
        ///     "orderCode": 20251012113337,
        ///     "amount": 10000,
        ///     "description": "CSO75IZQ5K2 Order O001",
        ///     "reference": "FT25286000022021",
        ///     "transactionDateTime": "2025-10-12 18:35:00",
        ///     "accountNumber": "0001447963672",
        ///     "currency": "VND",
        ///     "paymentLinkId": "486aec6cbada48bab51feda44eab5003",
        ///     "code": "00",
        ///     "desc": "success"
        ///   },
        ///   "signature": "3b3e29ae48c204160a42ddc4b648e68294f7b8b0a8e8c8a565b167b900e17aee"
        /// }
        /// ```
        /// 
        /// === RESPONSE ===
        /// Trả về thông tin booking đã được cập nhật với các trạng thái mới
        /// 
        /// === SECURITY ===
        /// - AllowAnonymous vì PayOS webhook gọi từ external
        /// - Nên verify signature trong production để đảm bảo request từ PayOS
        /// </remarks>
        [HttpPost("webhook/payos")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookRequest request)
        {
            var result = await _bookingService.HandlePayOSWebhookAsync(request);
            return StatusCode(result.StatusCode, result);
        }
    }
}
