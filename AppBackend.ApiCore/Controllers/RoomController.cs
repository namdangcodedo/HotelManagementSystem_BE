using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels.RoomModel;
using AppBackend.Services.Services.RoomServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing rooms and room types
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoomController : BaseApiController
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        #region ROOM TYPE CUSTOMER SEARCH

        /// <summary>
        /// [PUBLIC] Tìm kiếm loại phòng theo ngày check-in/out với số lượng phòng khả dụng
        /// </summary>
        /// <param name="request">Query parameters: checkInDate, checkOutDate (required), numberOfGuests, minPrice, maxPrice, bedType, minRoomSize, onlyActive (optional)</param>
        /// <returns>Danh sách loại phòng với số lượng phòng khả dụng cho khoảng thời gian đó</returns>
        /// <response code="200">Tìm kiếm thành công</response>
        /// <response code="400">CheckInDate hoặc CheckOutDate không hợp lệ</response>
        /// <remarks>
        /// ## 📋 Query Parameters
        ///
        /// | Parameter | Type | Required | Mô tả |
        /// |-----------|------|----------|-------|
        /// | `checkInDate` | datetime | ✅ **YES** | Ngày nhận phòng (format: yyyy-MM-dd, VD: 2025-12-20) |
        /// | `checkOutDate` | datetime | ✅ **YES** | Ngày trả phòng (format: yyyy-MM-dd, VD: 2025-12-22) |
        /// | `numberOfGuests` | int | ❌ | Số lượng khách (lọc phòng có sức chứa >= con số này) |
        /// | `minPrice` | decimal | ❌ | Giá tối thiểu mỗi đêm (VD: 500000) |
        /// | `maxPrice` | decimal | ❌ | Giá tối đa mỗi đêm (VD: 2000000) |
        /// | `bedType` | string | ❌ | Loại giường (King, Queen, Twin, Double...) |
        /// | `minRoomSize` | decimal | ❌ | Diện tích tối thiểu m² (VD: 30) |
        /// | `onlyActive` | bool | ❌ | Chỉ hiển thị phòng active (default: true) |
        ///
        /// ## 🔄 Ví dụ Request
        ///
        /// ```
        /// # Tìm tất cả phòng khả dụng từ 20/12 đến 22/12
        /// GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22
        ///
        /// # Tìm phòng cho 2 khách, giá 500k-2M, từ 20/12 đến 22/12
        /// GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-22&numberOfGuests=2&minPrice=500000&maxPrice=2000000
        ///
        /// # Tìm phòng King giá 1-2M từ 20/12 đến 23/12
        /// GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-23&bedType=King&minPrice=1000000&maxPrice=2000000
        ///
        /// # Tìm phòng 3+ khách, diện tích 40m² từ 20/12 đến 25/12
        /// GET /api/room/types/search?checkInDate=2025-12-20&checkOutDate=2025-12-25&numberOfGuests=3&minRoomSize=40
        /// ```
        ///
        /// ## 📤 Response Success (200)
        ///
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "responseCode": "SUCCESS",
        ///   "message": "Tìm thấy 5 loại phòng khả dụng từ 2025-12-20 đến 2025-12-22",
        ///   "statusCode": 200,
        ///   "data": [
        ///     {
        ///       "roomTypeId": 1,
        ///       "typeName": "Deluxe Room",
        ///       "typeCode": "DLX",
        ///       "description": "Phòng hướng biển với view tuyệt đẹp",
        ///       "basePriceNight": 1500000,
        ///       "maxOccupancy": 2,
        ///       "roomSize": 35.5,
        ///       "numberOfBeds": 1,
        ///       "bedType": "King",
        ///       "isActive": true,
        ///       "images": [
        ///         {
        ///           "mediumId": 1,
        ///           "filePath": "https://example.com/deluxe-1.jpg",
        ///           "description": "Room image",
        ///           "displayOrder": 0
        ///         }
        ///       ],
        ///       "amenities": [
        ///         {
        ///           "amenityId": 1,
        ///           "amenityName": "Tivi",
        ///           "amenityType": "Entertainment"
        ///         }
        ///       ],
        ///       "comments": [],
        ///       "totalRoomCount": 5,
        ///       "availableRoomCount": 3
        ///     },
        ///     {
        ///       "roomTypeId": 2,
        ///       "typeName": "Standard Room",
        ///       "typeCode": "STD",
        ///       "description": "Phòng tiêu chuẩn thoải mái",
        ///       "basePriceNight": 800000,
        ///       "maxOccupancy": 2,
        ///       "roomSize": 25.0,
        ///       "numberOfBeds": 1,
        ///       "bedType": "Double",
        ///       "isActive": true,
        ///       "images": [],
        ///       "amenities": [],
        ///       "comments": [],
        ///       "totalRoomCount": 8,
        ///       "availableRoomCount": 5
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🔑 Giải thích Response
        ///
        /// | Field | Mô tả |
        /// |-------|-------|
        /// | `totalRoomCount` | Tổng số phòng của loại này trong hệ thống |
        /// | `availableRoomCount` | **Số phòng KHẢ DỤNG** trong khoảng thời gian CheckIn-CheckOut |
        /// | `basePriceNight` | Giá/đêm (tính cho 1 phòng) |
        ///
        /// **Tính toán giá:**
        /// - Giá cho 1 đêm: `basePriceNight`
        /// - Giá cho toàn bộ stay: `basePriceNight × (số đêm)`
        ///
        /// VD: Check-in 20/12, Check-out 22/12 = 2 đêm
        /// - Deluxe: 1.500.000 × 2 = 3.000.000 VND
        /// - Standard: 800.000 × 2 = 1.600.000 VND
        ///
        /// ## ❌ Response Error (400)
        ///
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "responseCode": "INVALID_INPUT",
        ///   "message": "CheckInDate phải nhỏ hơn CheckOutDate",
        ///   "statusCode": 400,
        ///   "errors": ["Ngày check-in không hợp lệ"]
        /// }
        /// ```
        ///
        /// ## ❌ Response Error (404)
        ///
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "responseCode": "NOT_FOUND",
        ///   "message": "Không tìm thấy loại phòng nào khả dụng",
        ///   "statusCode": 404
        /// }
        /// ```
        ///
        /// ## 💡 Lưu ý quan trọng
        ///
        /// - **CheckInDate và CheckOutDate là bắt buộc** - cả hai phải được cung cấp
        /// - **Ngày check-out > check-in** - CheckOutDate phải sau CheckInDate
        /// - **Phòng khả dụng** = phòng không có booking nào trong khoảng thời gian đó
        /// - **AvailableRoomCount = 0** = loại phòng không còn phòng trống, có thể không hiển thị hoặc hiển thị dạng "Hết phòng"
        /// - Giá hiển thị là giá/đêm, FE cần tính tổng dựa trên số đêm lưu trú
        /// </remarks>
        [HttpGet("types/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchRoomTypes([FromQuery] SearchRoomTypeRequest request)
        {
            var result = await _roomService.SearchRoomTypesAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [PUBLIC] Lấy chi tiết loại phòng cho customer (có thể kiểm tra availability)
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <param name="checkInDate">Ngày check-in (optional)</param>
        /// <param name="checkOutDate">Ngày check-out (optional)</param>
        /// <returns>Thông tin chi tiết loại phòng kèm availability</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpGet("types/search/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomTypeDetailForCustomer(int id, [FromQuery] DateTime? checkInDate = null, [FromQuery] DateTime? checkOutDate = null)
        {
            var result = await _roomService.GetRoomTypeDetailForCustomerAsync(id, checkInDate, checkOutDate);
            return HandleResult(result);
        }

        #endregion

        #region ROOM TYPE ADMIN CRUD

        /// <summary>
        /// [ADMIN] Lấy danh sách loại phòng (không phân trang)
        /// </summary>
        /// <param name="request">Thông tin lọc</param>
        /// <returns>Danh sách loại phòng với hình ảnh và số lượng phòng</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("types")]
        public async Task<IActionResult> GetRoomTypeList([FromQuery] GetRoomTypeListRequest request)
        {
            var result = await _roomService.GetRoomTypeListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [ADMIN] Lấy chi tiết một loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Thông tin chi tiết loại phòng kèm hình ảnh</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpGet("types/{id}")]
        public async Task<IActionResult> GetRoomTypeDetail(int id)
        {
            var result = await _roomService.GetRoomTypeDetailAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Thêm loại phòng mới
        /// </summary>
        /// <param name="request">Thông tin loại phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin loại phòng đã thêm</returns>
        /// <response code="201">Thêm loại phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("types")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoomType([FromBody] AddRoomTypeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roomService.AddRoomTypeAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Cập nhật thông tin loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Thông tin loại phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        [HttpPut("types/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoomType(int id, [FromBody] UpdateRoomTypeRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.RoomTypeId = id;
            var result = await _roomService.UpdateRoomTypeAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Xóa loại phòng
        /// </summary>
        /// <param name="id">ID của loại phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy loại phòng</response>
        /// <response code="400">Không thể xóa vì còn phòng đang sử dụng loại này</response>
        [HttpDelete("types/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            var result = await _roomService.DeleteRoomTypeAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion

        #region ROOM CRUD - FOR ADMIN ONLY

        /// <summary>
        /// [ADMIN] Thêm phòng mới
        /// </summary>
        /// <param name="request">Thông tin phòng mới (bao gồm danh sách URL hình ảnh)</param>
        /// <returns>Thông tin phòng đã thêm</returns>
        /// <response code="201">Thêm phòng thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        [HttpPost("rooms")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddRoom([FromBody] AddRoomRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            var result = await _roomService.AddRoomAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Cập nhật thông tin phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <param name="request">Thông tin cập nhật (bao gồm danh sách URL hình ảnh mới nếu có)</param>
        /// <returns>Thông tin phòng đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpPut("rooms/{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.RoomId = id;
            var result = await _roomService.UpdateRoomAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [ADMIN] Xóa phòng
        /// </summary>
        /// <param name="id">ID của phòng</param>
        /// <returns>Kết quả thực hiện</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpDelete("rooms/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var result = await _roomService.DeleteRoomAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion
    }
}
