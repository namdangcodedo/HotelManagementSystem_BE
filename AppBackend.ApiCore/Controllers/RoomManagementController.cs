using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.RoomManagement;
using AppBackend.Services.ApiModels.RoomManagement;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý phòng - Chuyển đổi trạng thái, sơ đồ phòng, tìm kiếm
    /// </summary>
    [ApiController]
    [Route("api/RoomManagement")]
    public class RoomManagementController : BaseApiController
    {
        private readonly IRoomManagementService _roomManagementService;

        public RoomManagementController(IRoomManagementService roomManagementService)
        {
            _roomManagementService = roomManagementService;
        }

        /// <summary>
        /// [PUBLIC/ADMIN] Tìm kiếm và lọc phòng theo keyword, loại phòng, trạng thái
        /// </summary>
        /// <param name="request">Tiêu chí tìm kiếm (không phân trang - trả về tất cả kết quả)</param>
        /// <returns>Danh sách phòng phù hợp với filter</returns>
        /// <response code="200">Tìm kiếm thành công</response>
        /// <remarks>
        /// ## 📋 Query Parameters
        ///
        /// | Parameter | Type | Required | Mô tả |
        /// |-----------|------|----------|-------|
        /// | `roomName` | string | ❌ | Tìm kiếm theo tên phòng (VD: "Phòng 101", "101") |
        /// | `roomTypeId` | int | ❌ | Lọc theo ID loại phòng |
        /// | `statusId` | int | ❌ | Lọc theo ID trạng thái (từ CommonCodes) |
        /// | `floor` | int | ❌ | Lọc theo tầng (VD: 1, 2, 3...) |
        /// | `minPrice` | decimal | ❌ | Giá tối thiểu mỗi đêm |
        /// | `maxPrice` | decimal | ❌ | Giá tối đa mỗi đêm |
        ///
        /// ## 🔄 Ví dụ Request
        ///
        /// ```
        /// # Tìm tất cả phòng
        /// GET /api/rooms/search
        ///
        /// # Tìm phòng có tên chứa "101"
        /// GET /api/rooms/search?roomName=101
        ///
        /// # Tìm phòng trạng thái "Available" (StatusId=1) của tầng 1
        /// GET /api/rooms/search?statusId=1&floor=1
        ///
        /// # Tìm phòng loại "Deluxe" (RoomTypeId=3) có giá 1-2 triệu
        /// GET /api/rooms/search?roomTypeId=3&minPrice=1000000&maxPrice=2000000
        ///
        /// # Combo: tầng 2 + loại phòng + trạng thái + tên
        /// GET /api/rooms/search?floor=2&roomTypeId=2&statusId=2&roomName=Phòng
        /// ```
        ///
        /// ## 📤 Response Success (200)
        ///
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "responseCode": "SUCCESS",
        ///   "message": "Tìm thấy 5 phòng",
        ///   "statusCode": 200,
        ///   "data": {
        ///     "rooms": [
        ///       {
        ///         "roomId": 1,
        ///         "roomName": "Phòng 101",
        ///         "roomTypeId": 1,
        ///         "roomTypeName": "Deluxe",
        ///         "roomTypeCode": "DLX",
        ///         "basePriceNight": 1500000,
        ///         "statusId": 1,
        ///         "status": "Available",
        ///         "statusCode": "AVAILABLE",
        ///         "description": "Phòng hướng biển với view tuyệt đẹp",
        ///         "maxOccupancy": 2,
        ///         "roomSize": 35.5,
        ///         "numberOfBeds": 1,
        ///         "bedType": "King",
        ///         "images": [
        ///           "https://example.com/room101-1.jpg",
        ///           "https://example.com/room101-2.jpg"
        ///         ],
        ///         "createdAt": "2024-01-15T10:30:00Z",
        ///         "updatedAt": "2025-12-11T14:20:00Z"
        ///       },
        ///       {
        ///         "roomId": 2,
        ///         "roomName": "Phòng 102",
        ///         "roomTypeId": 1,
        ///         "roomTypeName": "Deluxe",
        ///         "roomTypeCode": "DLX",
        ///         "basePriceNight": 1500000,
        ///         "statusId": 3,
        ///         "status": "Occupied",
        ///         "statusCode": "OCCUPIED",
        ///         "description": "Phòng hướng biển",
        ///         "maxOccupancy": 2,
        ///         "roomSize": 35.5,
        ///         "numberOfBeds": 1,
        ///         "bedType": "King",
        ///         "images": [],
        ///         "createdAt": "2024-01-15T10:30:00Z",
        ///         "updatedAt": "2025-12-11T14:20:00Z"
        ///       }
        ///     ],
        ///     "totalRecords": 2,
        ///     "pageNumber": 1,
        ///     "pageSize": 2,
        ///     "totalPages": 1
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ Response Error (400)
        ///
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "responseCode": "INVALID_INPUT",
        ///   "message": "Dữ liệu không hợp lệ",
        ///   "statusCode": 400
        /// }
        /// ```
        ///
        /// ## 💡 Status Codes Thường Dùng (từ CommonCodes)
        ///
        /// | StatusId | Status | Code | Mô tả |
        /// |----------|--------|------|-------|
        /// | 1 | Available | AVAILABLE | Phòng trống, sẵn sàng cho khách |
        /// | 2 | Booked | BOOKED | Phòng đã được đặt |
        /// | 3 | Occupied | OCCUPIED | Khách đang sử dụng phòng |
        /// | 4 | Cleaning | CLEANING | Phòng đang được dọn dẹp |
        /// | 5 | Maintenance | MAINTENANCE | Phòng đang bảo trì |
        /// | 6 | PendingInspection | PENDING_INSPECTION | Chờ kiểm tra |
        /// | 7 | OutOfService | OUT_OF_SERVICE | Phòng ngừng hoạt động tạm thời |
        ///
        /// 💡 **Lấy danh sách Status động:** `GET /api/commoncode?codeType=RoomStatus`
        /// </remarks>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RoomListResponse), 200)]
        public async Task<IActionResult> SearchRooms([FromQuery] SearchRoomsRequest request)
        {
            var result = await _roomManagementService.SearchRoomsAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// [PUBLIC/ADMIN] Lấy chi tiết của một phòng cụ thể
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Chi tiết phòng</returns>
        /// <response code="200">Lấy thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// Lấy toàn bộ thông tin chi tiết của 1 phòng bao gồm:
        /// - Thông tin cơ bản (tên, loại phòng, trạng thái, giá)
        /// - Mô tả, kích thước, số giường
        /// - Hình ảnh, amenities
        /// - Thời gian tạo/cập nhật
        ///
        /// ## 📤 Response Example
        /// ```json
        /// {
        ///   "roomId": 101,
        ///   "roomName": "Phòng 101",
        ///   "roomTypeId": 1,
        ///   "roomTypeName": "Deluxe",
        ///   "roomTypeCode": "DLX",
        ///   "basePriceNight": 1500000,
        ///   "statusId": 1,
        ///   "status": "Available",
        ///   "statusCode": "AVL",
        ///   "description": "Phòng hướng biển với view tuyệt đẹp",
        ///   "maxOccupancy": 2,
        ///   "roomSize": 35.5,
        ///   "numberOfBeds": 1,
        ///   "bedType": "King",
        ///   "images": [
        ///     {
        ///       "mediumId": 1,
        ///       "filePath": "https://example.com/room101-1.jpg",
        ///       "description": "Room photo",
        ///       "displayOrder": 1
        ///     }
        ///   ],
        ///   "createdAt": "2024-01-15T10:30:00Z",
        ///   "updatedAt": "2025-12-11T14:20:00Z"
        /// }
        /// ```
        /// </remarks>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RoomDetailDto), 200)]
        public async Task<IActionResult> GetRoomDetail(int id)
        {
            var result = await _roomManagementService.GetRoomDetailAsync(id);
            return HandleResult(result);
        }

        #region Manager/Receptionist - Chuyển đổi trạng thái phòng

        /// <summary>
        /// [Manager/Receptionist] Thay đổi trạng thái phòng
        /// </summary>
        /// <param name="request">Thông tin thay đổi trạng thái</param>
        /// <returns>Kết quả thay đổi</returns>
        /// <response code="200">Thay đổi thành công</response>
        /// <response code="400">StatusId không hợp lệ</response>
        /// <response code="403">Không có quyền</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// **Quyền hạn:**
        /// - **Manager**: Có thể chuyển tất cả trạng thái
        /// - **Receptionist**: Chỉ có thể chuyển Available ↔ Booked ↔ Occupied
        /// 
        /// **Request Body Example:**
        /// ```json
        /// {
        ///   "roomId": 1,
        ///   "newStatusId": 2,
        ///   "reason": "Khách đặt phòng qua điện thoại"
        /// }
        /// ```
        /// 
        /// **Các CommonCodeId cho RoomStatus:**
        /// - 1: Available (Trống)
        /// - 2: Booked (Đã đặt)
        /// - 3: Occupied (Đang sử dụng)
        /// - 4: Cleaning (Đang dọn)
        /// - 5: Maintenance (Bảo trì)
        /// - 6: PendingInspection (Chờ kiểm tra)
        /// - 7: OutOfService (Ngừng hoạt động)
        /// 
        /// **Lưu ý:** UI nên gọi API GET /api/commoncode?codeType=RoomStatus để lấy danh sách động
        /// </remarks>
        [HttpPatch("status")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> ChangeRoomStatus([FromBody] ChangeRoomStatusRequest request)
        {
            var userRole = CurrentUserRoles.FirstOrDefault() ?? "User";
            var result = await _roomManagementService.ChangeRoomStatusAsync(request, CurrentUserId, userRole);
            return HandleResult(result);
        }

        /// <summary>
        /// [Manager only] Thay đổi trạng thái nhiều phòng cùng lúc
        /// </summary>
        /// <param name="request">Danh sách phòng và trạng thái mới</param>
        /// <returns>Kết quả thay đổi hàng loạt</returns>
        /// <response code="200">Thay đổi thành công</response>
        /// <response code="400">StatusId không hợp lệ</response>
        /// <response code="403">Không có quyền</response>
        /// <remarks>
        /// **Request Body Example:**
        /// ```json
        /// {
        ///   "roomIds": [1, 2, 3],
        ///   "newStatusId": 4,
        ///   "reason": "Dọn phòng sau check-out hàng loạt"
        /// }
        /// ```
        /// </remarks>
        [HttpPatch("bulk-status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> BulkChangeRoomStatus([FromBody] BulkChangeRoomStatusRequest request)
        {
            var result = await _roomManagementService.BulkChangeRoomStatusAsync(request, CurrentUserId);
            return HandleResult(result);
        }

        #endregion

        #region Housekeeper - Dọn dẹp phòng

        /// <summary>
        /// [Housekeeper] Bắt đầu dọn phòng
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Kết quả</returns>
        /// <response code="200">Đánh dấu thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// Chuyển trạng thái phòng sang **Cleaning** (Đang dọn dẹp).
        /// 
        /// **Flow:**
        /// 1. Housekeeper nhận nhiệm vụ dọn phòng
        /// 2. Gọi API này để đánh dấu bắt đầu
        /// 3. Phòng chuyển sang trạng thái "Cleaning"
        /// 4. Sau khi dọn xong, gọi API "complete-cleaning"
        /// </remarks>
        [HttpPost("{id}/start-cleaning")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<IActionResult> StartCleaning(int id)
        {
            var result = await _roomManagementService.MarkRoomAsCleaningAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// [Housekeeper] Hoàn tất dọn phòng
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Kết quả</returns>
        /// <response code="200">Hoàn tất thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// Chuyển trạng thái phòng từ **Cleaning** → **Available** (Trống, sẵn sàng).
        /// 
        /// Phòng sau khi dọn xong sẽ sẵn sàng cho khách thuê tiếp theo.
        /// </remarks>
        [HttpPost("{id}/complete-cleaning")]
        [Authorize(Roles = "Admin,Manager,Housekeeper")]
        public async Task<IActionResult> CompleteCleaning(int id)
        {
            var result = await _roomManagementService.MarkRoomAsCleanedAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion

        #region Technician - Bảo trì phòng

        /// <summary>
        /// [Technician/Manager] Đánh dấu phòng cần bảo trì
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <param name="reason">Lý do bảo trì</param>
        /// <returns>Kết quả</returns>
        /// <response code="200">Đánh dấu thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// Chuyển trạng thái phòng sang **Maintenance** (Bảo trì).
        /// 
        /// Phòng bảo trì sẽ không hiển thị cho khách đặt.
        /// 
        /// **Ví dụ lý do:**
        /// - Sửa điều hòa
        /// - Thay đồ nội thất
        /// - Sơn lại tường
        /// </remarks>
        [HttpPost("{id}/start-maintenance")]
        [Authorize(Roles = "Admin,Manager,Technician")]
        public async Task<IActionResult> StartMaintenance(int id, [FromBody] string? reason = null)
        {
            var result = await _roomManagementService.MarkRoomForMaintenanceAsync(id, CurrentUserId, reason ?? "");
            return HandleResult(result);
        }

        /// <summary>
        /// [Technician/Manager] Hoàn tất bảo trì phòng
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Kết quả</returns>
        /// <response code="200">Hoàn tất thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        /// <remarks>
        /// Chuyển trạng thái phòng từ **Maintenance** → **Available** (Trống, sẵn sàng).
        /// 
        /// Phòng sau khi bảo trì xong sẽ sẵn sàng cho khách thuê.
        /// </remarks>
        [HttpPost("{id}/complete-maintenance")]
        [Authorize(Roles = "Admin,Manager,Technician")]
        public async Task<IActionResult> CompleteMaintenance(int id)
        {
            var result = await _roomManagementService.CompleteMaintenanceAsync(id, CurrentUserId);
            return HandleResult(result);
        }

        #endregion
    }
}
