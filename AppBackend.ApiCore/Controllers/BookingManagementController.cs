using AppBackend.Services.ApiModels.BookingModel;
using AppBackend.Services.Services.BookingServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý booking offline - dành cho Lễ tân, Manager, Admin
    /// Lễ tân tự chọn phòng cụ thể, không tự động chọn phòng
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BookingManagementController : BaseApiController
    {
        private readonly IBookingManagementService _bookingManagementService;

        public BookingManagementController(IBookingManagementService bookingManagementService)
        {
            _bookingManagementService = bookingManagementService;
        }

        /// <summary>
        /// Search và filter phòng available - API GET chuẩn
        /// </summary>
        /// <remarks>
        /// API GET để tìm kiếm và filter phòng trống với đầy đủ tiêu chí:
        /// - Ngày check-in/check-out
        /// - Loại phòng (RoomType)
        /// - Số lượng giường
        /// - Loại giường (King, Queen, Twin...)
        /// - Số người tối đa
        /// - Khoảng giá
        /// - Diện tích phòng
        /// - Tìm kiếm theo tên/mã phòng
        /// - Sắp xếp theo giá, diện tích, tên
        /// 
        /// ### Query Parameters Examples:
        /// 
        /// **Tìm phòng trống trong khoảng thời gian:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?checkInDate=2025-12-10&checkOutDate=2025-12-12
        /// ```
        /// 
        /// **Filter theo loại phòng và số giường:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?roomTypeId=1&numberOfBeds=2
        /// ```
        /// 
        /// **Filter theo giá:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?minPrice=500000&maxPrice=2000000
        /// ```
        /// 
        /// **Filter theo loại giường:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?bedType=King
        /// ```
        /// 
        /// **Filter theo số người:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?maxOccupancy=4
        /// ```
        /// 
        /// **Search theo tên:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?searchTerm=deluxe
        /// ```
        /// 
        /// **Kết hợp nhiều filter:**
        /// ```
        /// GET /api/BookingManagement/rooms/search?checkInDate=2025-12-10&checkOutDate=2025-12-12&roomTypeId=1&numberOfBeds=2&minPrice=1000000&maxPrice=3000000&sortBy=price&pageNumber=1&pageSize=10
        /// ```
        /// 
        /// ### Sorting Options:
        /// - `sortBy=price` - Sắp xếp theo giá
        /// - `sortBy=roomsize` - Sắp xếp theo diện tích
        /// - `sortBy=roomname` - Sắp xếp theo tên phòng
        /// - `isDescending=true` - Sắp xếp giảm dần
        /// 
        /// ### Response bao gồm:
        /// - Danh sách phòng chi tiết (Room info, RoomType, Price, Size, Beds...)
        /// - Amenities của từng phòng
        /// - Hình ảnh phòng
        /// - Pagination info
        /// </remarks>
        [HttpGet("rooms/search")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> SearchAvailableRooms([FromQuery] SearchAvailableRoomsRequest request)
        {
            var result = await _bookingManagementService.SearchAvailableRoomsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Check phòng trống theo loại phòng và số lượng (dành cho lễ tân trước khi tạo booking)
        /// </summary>
        /// <remarks>
        /// Dữ liệu đầu vào giống booking online:
        /// {
        ///   "checkInDate": "2025-12-14T15:02",
        ///   "checkOutDate": "2025-12-21T15:02",
        ///   "roomTypes": [
        ///     { "roomTypeId": 1, "quantity": 1 },
        ///     { "roomTypeId": 2, "quantity": 1 }
        ///   ]
        /// }
        /// </remarks>
        [HttpPost("available-rooms")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CheckAvailableRooms([FromBody] CheckRoomAvailabilityRequest request)
        {
            var result = await _bookingManagementService.CheckAvailableRoomsAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tạo booking offline - Lễ tân tự chọn phòng cụ thể
        /// </summary>
        /// <remarks>
        /// ### Đặc điểm:
        /// - Lễ tân **tự chọn các phòng cụ thể** bằng RoomIds (không tự động)
        /// - BookingType = "WalkIn"
        /// - Status mặc định = "CheckedIn"
        /// - Thanh toán toàn bộ ngay tại quầy
        /// - Tự động gửi email xác nhận
        /// 
        /// ### Request Example:
        /// ```json
        /// {
        ///   "fullName": "Nguyễn Văn A",
        ///   "email": "nguyenvana@gmail.com",
        ///   "phoneNumber": "0901234567",
        ///   "identityCard": "001234567890",
        ///   "address": "123 Đường ABC, TP.HCM",
        ///   "roomIds": [101, 102, 201],
        ///   "checkInDate": "2025-12-10T14:00:00Z",
        ///   "checkOutDate": "2025-12-12T12:00:00Z",
        ///   "specialRequests": "Phòng tầng cao, view đẹp",
        ///   "paymentMethod": "Cash"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("offline")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CreateOfflineBooking([FromBody] CreateOfflineBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.CreateOfflineBookingAsync(request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cập nhật thông tin booking offline
        /// </summary>
        [HttpPut("offline/{bookingId}")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> UpdateOfflineBooking(int bookingId, [FromBody] UpdateOfflineBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.UpdateOfflineBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy danh sách booking với filter (online/offline)
        /// </summary>
        /// <remarks>
        /// ### Query Parameters:
        /// - **fromDate**: Lọc từ ngày (optional)
        /// - **toDate**: Lọc đến ngày (optional)
        /// - **bookingStatus**: Lọc theo trạng thái từ CommonCode (optional) - ID của CommonCode
        /// - **bookingType**: Lọc loại booking từ CommonCode (optional) - ID của CommonCode
        ///   - Không truyền hoặc null = Lấy tất cả
        ///   - Truyền ID của "Online" = Chỉ lấy booking online
        ///   - Truyền ID của "WalkIn" = Chỉ lấy booking offline
        /// - **key**: Tìm kiếm theo tên khách, email, số điện thoại (optional)
        /// - **pageNumber**: Trang hiện tại (default: 1)
        /// - **pageSize**: Số records mỗi trang (default: 20)
        /// 
        /// ### Examples:
        /// 
        /// **Lấy tất cả booking:**
        /// ```
        /// GET /api/BookingManagement/bookings?pageNumber=1&pageSize=20
        /// ```
        /// 
        /// **Lọc booking offline:**
        /// ```
        /// GET /api/BookingManagement/bookings?bookingType=2&pageNumber=1&pageSize=20
        /// ```
        /// 
        /// **Lọc theo ngày và trạng thái:**
        /// ```
        /// GET /api/BookingManagement/bookings?fromDate=2024-12-01&toDate=2024-12-31&bookingStatus=3
        /// ```
        /// 
        /// **Tìm kiếm khách hàng:**
        /// ```
        /// GET /api/BookingManagement/bookings?key=nguyen van a
        /// ```
        /// </remarks>
        [HttpGet("bookings")]
        public async Task<IActionResult> GetBookings([FromQuery] BookingFilterRequest filter)
        {
            var result = await _bookingManagementService.GetBookingsAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy chi tiết một booking
        /// </summary>
        [HttpGet("{bookingId}")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetBookingDetail(int bookingId)
        {
            var result = await _bookingManagementService.GetBookingDetailAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hủy booking với lý do
        /// </summary>
        /// <remarks>
        /// ### Request Example:
        /// ```json
        /// {
        ///   "reason": "Khách yêu cầu hủy do thay đổi lịch trình"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/cancel")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CancelBooking(int bookingId, [FromBody] CancelBookingRequest request)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.CancelBookingAsync(bookingId, request, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Check-in booking - Chuyển trạng thái booking sang "CheckedIn"
        /// </summary>
        /// <remarks>
        /// ### Mục đích:
        /// API này được sử dụng khi khách đến khách sạn và làm thủ tục check-in.
        /// 
        /// ### Workflow:
        /// 1. **Khách đến quầy lễ tân** vào ngày check-in
        /// 2. **Lễ tán kiểm tra booking** (qua BookingId hoặc tìm kiếm khách hàng)
        /// 3. **Xác nhận thông tin** khách hàng và phòng
        /// 4. **Gọi API này** để chuyển trạng thái booking sang **CheckedIn**
        /// 5. Hệ thống tự động:
        ///    - Cập nhật booking status → **CheckedIn**
        ///    - Cập nhật room status → **Occupied** (Đang sử dụng)
        ///    - Ghi nhận thời gian check-in thực tế
        ///    - Gửi email welcome cho khách (optional)
        /// 
        /// ### Business Rules:
        /// - Chỉ booking có status **Confirmed** hoặc **Pending** mới có thể check-in
        /// - Không thể check-in booking đã **Cancelled** hoặc **CheckedOut**
        /// - Check-in date phải trong khoảng hợp lệ (không quá sớm hoặc quá muộn)
        /// 
        /// ### Authorization:
        /// - **Receptionist**, **Manager**, **Admin**
        /// 
        /// ### Example:
        /// ```
        /// POST /api/BookingManagement/123/check-in
        /// ```
        /// 
        /// ### Response Success:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Check-in thành công. Chúc quý khách có kỳ nghỉ vui vẻ!",
        ///   "data": {
        ///     "bookingId": 123,
        ///     "checkInTime": "2025-12-09T14:30:00Z",
        ///     "roomNumbers": ["101", "102"],
        ///     "customerName": "Nguyễn Văn A",
        ///     "checkOutDate": "2025-12-11T12:00:00Z"
        ///   }
        /// }
        /// ```
        /// 
        /// ### Response Error - Booking không hợp lệ:
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "statusCode": 400,
        ///   "message": "Không thể check-in. Booking đang ở trạng thái: Cancelled"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/check-in")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> CheckInBooking(int bookingId)
        {
            var employeeId = CurrentUserId;
            var result = await _bookingManagementService.CheckInBookingAsync(bookingId, employeeId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Lấy thông tin QR payment cho booking
        /// </summary>
        /// <remarks>
        /// Nhân viên có thể generate QR code để khách thanh toán qua VietQR
        /// Chỉ áp dụng cho booking có status = "Pending"
        /// </remarks>
        [HttpGet("{bookingId}/qr-payment")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> GetQRPaymentInfo(int bookingId)
        {
            var result = await _bookingManagementService.GetQRPaymentInfoAsync(bookingId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Manager/Admin xác nhận đã nhận được tiền cọc từ khách (sau khi check bill ngân hàng)
        /// </summary>
        /// <remarks>
        /// ### Luồng xử lý:
        /// 1. Khách báo "đã chuyển khoản" → Status chuyển sang **PendingConfirmation**
        /// 2. Manager vào app ngân hàng kiểm tra bill
        /// 3. Manager gọi API này để xác nhận → Status chuyển sang **Confirmed**
        /// 4. Hệ thống tự động:
        ///    - Tạo transaction record
        ///    - **Gửi email cảm ơn + thông tin đặt phòng chi tiết cho khách**
        ///    - Email bao gồm: Booking ID, thông tin phòng, ngày check-in/out, tổng tiền, link xem chi tiết
        /// 
        /// ### Authorization:
        /// - Chỉ **Manager** và **Admin** mới có quyền confirm
        /// - Lễ tân (Receptionist) **KHÔNG** có quyền confirm payment
        /// 
        /// ### Example:
        /// ```
        /// POST /api/BookingManagement/123/confirm-payment
        /// ```
        /// 
        /// ### Response Success:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Xác nhận thanh toán thành công. Email đã được gửi đến khách hàng."
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/confirm-payment")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> ConfirmPayment(int bookingId)
        {
            // Get current user (Manager/Admin confirming the deposit)
            var confirmedBy = CurrentUserId > 0 ? CurrentUserId : (int?)null;

            // Gọi service ConfirmOnlineBookingAsync - tạo Transaction cho deposit
            var result = await _bookingManagementService.ConfirmOnlineBookingAsync(bookingId, confirmedBy);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Tìm kiếm nhanh khách hàng theo số điện thoại, email hoặc tên
        /// </summary>
        /// <remarks>
        /// ### Mục đích:
        /// API này giúp lễ tân/nhân viên **tìm kiếm nhanh thông tin khách hàng** khi tạo booking offline.
        /// 
        /// ### Luồng sử dụng:
        /// 1. **Khách đến quầy đặt phòng**
        /// 2. **Lễ tân hỏi số điện thoại/email** của khách
        /// 3. **Gọi API này** với searchKey = số điện thoại/email/tên
        /// 4. **Nếu tìm thấy:**
        ///    - Frontend tự động **fill thông tin** khách hàng vào form (FullName, Phone, Email, Address, IdentityCard)
        ///    - Hiển thị thống kê: Tổng số booking trước đó, ngày booking gần nhất
        ///    - Lễ tân chỉ cần chọn phòng và confirm → Tạo booking nhanh
        /// 5. **Nếu không tìm thấy:**
        ///    - Lễ tân nhập thông tin mới
        ///    - Khi tạo booking, hệ thống sẽ **tự động tạo customer mới**
        /// 
        /// ### Query Parameters:
        /// - **searchKey**: Số điện thoại, email hoặc tên khách hàng (required)
        /// 
        /// ### Examples:
        /// 
        /// **Tìm theo số điện thoại:**
        /// ```
        /// GET /api/BookingManagement/customers/quick-search?searchKey=0901234567
        /// ```
        /// 
        /// **Tìm theo email:**
        /// ```
        /// GET /api/BookingManagement/customers/quick-search?searchKey=customer@gmail.com
        /// ```
        /// 
        /// **Tìm theo tên:**
        /// ```
        /// GET /api/BookingManagement/customers/quick-search?searchKey=Nguyen Van A
        /// ```
        /// 
        /// ### Response Success - Tìm thấy:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Tìm thấy 2 khách hàng",
        ///   "data": [
        ///     {
        ///       "customerId": 123,
        ///       "fullName": "Nguyễn Văn A",
        ///       "phoneNumber": "0901234567",
        ///       "email": "nguyenvana@gmail.com",
        ///       "identityCard": "001234567890",
        ///       "address": "123 Đường ABC, TP.HCM",
        ///       "totalBookings": 5,
        ///       "lastBookingDate": "2024-11-20T10:30:00Z",
        ///       "matchedBy": "Phone"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// ### Response Success - Không tìm thấy:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Không tìm thấy khách hàng. Vui lòng nhập thông tin mới để tạo booking.",
        ///   "data": []
        /// }
        /// ```
        /// 
        /// ### Response Fields:
        /// - **matchedBy**: "Phone" | "Email" | "Name" - Để frontend biết highlight field nào
        /// - **totalBookings**: Số lần đã đặt phòng trước đó
        /// - **lastBookingDate**: Ngày booking gần nhất (để biết khách quen hay khách mới)
        /// 
        /// ### Notes:
        /// - API trả về **tối đa 10 kết quả** để tránh quá nhiều
        /// - Search **không phân biệt hoa thường**
        /// - Hỗ trợ **search một phần** (ví dụ: "090" sẽ tìm thấy "0901234567")
        /// </remarks>
        [HttpGet("customers/quick-search")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> QuickSearchCustomer([FromQuery] string searchKey)
        {
            var result = await _bookingManagementService.QuickSearchCustomerAsync(searchKey);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Add dịch vụ vào booking trong quá trình khách ở
        /// </summary>
        /// <remarks>
        /// API để thêm các dịch vụ vào booking trong quá trình khách ở (sau khi check-in hoặc confirmed).
        ///
        /// ### Workflow:
        /// 1. Khách ở trong khách sạn và sử dụng các dịch vụ: minibar, giặt ủi, spa, massage, v.v.
        /// 2. Nhân viên sử dụng API này để add các services vào booking
        /// 3. Services sẽ được tính vào hóa đơn khi checkout
        /// 4. API preview checkout sẽ hiển thị tất cả services đã add
        ///
        /// ### Service Types:
        /// - **Room Service** (có `bookingRoomId`): Dịch vụ gắn với phòng cụ thể (minibar, giặt ủi theo phòng, late checkout)
        /// - **Booking Service** (`bookingRoomId = null`): Dịch vụ chung cho booking (spa, massage, tour, ăn uống)
        ///
        /// ### Example Request:
        /// ```json
        /// {
        ///   "bookingId": 123,
        ///   "services": [
        ///     {
        ///       "serviceId": 5,
        ///       "quantity": 2,
        ///       "bookingRoomId": 456,
        ///       "note": "Minibar - 2 bia Heineken"
        ///     },
        ///     {
        ///       "serviceId": 12,
        ///       "quantity": 1,
        ///       "bookingRoomId": null,
        ///       "note": "Massage 60 phút"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ### Response:
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "statusCode": 200,
        ///   "message": "Đã thêm 2 dịch vụ vào booking",
        ///   "data": {
        ///     "bookingId": 123,
        ///     "addedServices": [
        ///       {
        ///         "serviceName": "Minibar",
        ///         "roomId": 201,
        ///         "quantity": 2,
        ///         "price": 50000,
        ///         "subTotal": 100000,
        ///         "type": "RoomService"
        ///       },
        ///       {
        ///         "serviceName": "Massage 60 phút",
        ///         "quantity": 1,
        ///         "price": 500000,
        ///         "subTotal": 500000,
        ///         "type": "BookingService"
        ///       }
        ///     ],
        ///     "addedBy": 3,
        ///     "addedAt": "2025-12-08T10:30:00Z"
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPost("{bookingId}/services")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> AddServicesToBooking(int bookingId, [FromBody] AddBookingServiceRequest request)
        {
            // Validate request
            if (request == null || request.Services == null || !request.Services.Any())
            {
                return ValidationError("Danh sách dịch vụ không hợp lệ");
            }

            // Ensure bookingId in route matches request
            request.BookingId = bookingId;

            // Get current employee ID
            var employeeId = CurrentUserId > 0 ? CurrentUserId : (int?)null;

            var result = await _bookingManagementService.AddServicesToBookingAsync(request, employeeId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
