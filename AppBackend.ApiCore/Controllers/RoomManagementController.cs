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
    [Route("api/rooms")]
    [Authorize]
    public class RoomManagementController : BaseApiController
    {
        private readonly IRoomManagementService _roomManagementService;

        public RoomManagementController(IRoomManagementService roomManagementService)
        {
            _roomManagementService = roomManagementService;
        }

        /// <summary>
        /// Tìm kiếm và lọc phòng (All roles)
        /// </summary>
        /// <param name="request">Tiêu chí tìm kiếm</param>
        /// <returns>Danh sách phòng có phân trang</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(RoomListResponse), 200)]
        public async Task<IActionResult> SearchRooms([FromQuery] SearchRoomsRequest request)
        {
            var result = await _roomManagementService.SearchRoomsAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy sơ đồ phòng theo tầng (All roles)
        /// </summary>
        /// <param name="floor">Số tầng (null = tất cả tầng)</param>
        /// <returns>Sơ đồ phòng với trạng thái</returns>
        /// <response code="200">Lấy sơ đồ thành công</response>
        /// <remarks>
        /// Trả về sơ đồ phòng để hiển thị UI dạng grid/map.
        /// 
        /// **Ví dụ:**
        /// - GET /api/rooms/map → Tất cả tầng
        /// - GET /api/rooms/map?floor=1 → Chỉ tầng 1
        /// - GET /api/rooms/map?floor=2 → Chỉ tầng 2
        /// </remarks>
        [HttpGet("map")]
        [ProducesResponseType(typeof(List<RoomMapResponse>), 200)]
        public async Task<IActionResult> GetRoomMap([FromQuery] int? floor = null)
        {
            var result = await _roomManagementService.GetRoomMapAsync(floor);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy chi tiết một phòng (All roles)
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Thông tin chi tiết phòng</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoomDetailDto), 200)]
        public async Task<IActionResult> GetRoomDetail(int id)
        {
            var result = await _roomManagementService.GetRoomDetailAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy thống kê trạng thái phòng (All roles)
        /// </summary>
        /// <returns>Tổng hợp số lượng phòng theo trạng thái</returns>
        /// <response code="200">Lấy thống kê thành công</response>
        [HttpGet("stats")]
        public async Task<IActionResult> GetRoomStatusSummary()
        {
            var result = await _roomManagementService.GetRoomStatusSummaryAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Lấy danh sách trạng thái có thể chuyển đổi (theo role hiện tại)
        /// </summary>
        /// <param name="id">Room ID</param>
        /// <returns>Danh sách trạng thái có thể chuyển</returns>
        /// <response code="200">Lấy danh sách thành công</response>
        /// <response code="404">Không tìm thấy phòng</response>
        [HttpGet("{id}/available-status")]
        public async Task<IActionResult> GetAvailableStatusTransitions(int id)
        {
            var userRole = CurrentUserRoles.FirstOrDefault() ?? "User";
            var result = await _roomManagementService.GetAvailableStatusTransitionsAsync(id, userRole);
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
