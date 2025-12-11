using System.ComponentModel.DataAnnotations;
using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.RoomManagement
{
    /// <summary>
    /// Request để thay đổi trạng thái phòng
    /// </summary>
    public class ChangeRoomStatusRequest
    {
        [Required(ErrorMessage = "RoomId là bắt buộc")]
        public int RoomId { get; set; }

        /// <summary>
        /// CommonCodeId của trạng thái mới (từ bảng CommonCodes với CodeType = "RoomStatus")
        /// </summary>
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "NewStatusId phải lớn hơn 0")]
        public int NewStatusId { get; set; }

        /// <summary>
        /// Lý do thay đổi trạng thái (optional)
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Request để thay đổi trạng thái nhiều phòng cùng lúc
    /// </summary>
    public class BulkChangeRoomStatusRequest
    {
        [Required(ErrorMessage = "Danh sách RoomIds là bắt buộc")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 phòng")]
        public List<int> RoomIds { get; set; } = new();

        /// <summary>
        /// CommonCodeId của trạng thái mới (từ bảng CommonCodes với CodeType = "RoomStatus")
        /// </summary>
        [Required(ErrorMessage = "Trạng thái mới là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "NewStatusId phải lớn hơn 0")]
        public int NewStatusId { get; set; }

        /// <summary>
        /// Lý do thay đổi trạng thái (optional)
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO hiển thị thông tin phòng chi tiết
    /// </summary>
    public class RoomDetailDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public decimal BasePriceNight { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        /// <summary>
        /// Images with metadata for CRUD operations
        /// </summary>
        public List<MediumDto> Images { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO hiển thị sơ đồ phòng (Room Map)
    /// </summary>
    public class RoomMapDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomTypeCode { get; set; } = string.Empty;
        public string RoomTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public int Floor { get; set; }
        public decimal BasePriceNight { get; set; }
    }

    /// <summary>
    /// Request tìm kiếm và lọc phòng
    /// </summary>
    public class SearchRoomsRequest
    {
        public string? RoomName { get; set; }
        public int? RoomTypeId { get; set; }
        
        /// <summary>
        /// Lọc theo StatusId (CommonCodeId) - khuyến nghị dùng cái này
        /// </summary>
        public int? StatusId { get; set; }
        
        /// <summary>
        /// Lọc theo Status name (backward compatibility) - deprecated
        /// </summary>
        [Obsolete("Sử dụng StatusId thay vì Status string để tránh lỗi và tăng performance")]
        public string? Status { get; set; }
        
        public int? Floor { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response danh sách phòng có phân trang
    /// </summary>
    public class RoomListResponse
    {
        public List<RoomDetailDto> Rooms { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Response sơ đồ phòng theo tầng
    /// </summary>
    public class RoomMapResponse
    {
        public int Floor { get; set; }
        public List<RoomMapDto> Rooms { get; set; } = new();
        public Dictionary<string, int> StatusSummary { get; set; } = new();
    }

    /// <summary>
    /// Request gán phòng cho nhân viên dọn dẹp (Housekeeper)
    /// </summary>
    public class AssignRoomToHousekeeperRequest
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public int HousekeeperId { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Thống kê trạng thái phòng
    /// </summary>
    public class RoomStatusSummaryDto
    {
        public string Status { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Log lịch sử thay đổi trạng thái phòng
    /// </summary>
    public class RoomStatusHistoryDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string ChangedByUser { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
