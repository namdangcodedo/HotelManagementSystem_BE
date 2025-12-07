using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.RoomModel
{
    // ============= ROOM TYPE SEARCH (FOR CUSTOMER) =============
    /// <summary>
    /// Request để customer tìm kiếm loại phòng với các filter
    /// </summary>
    public class SearchRoomTypeRequest : PagedRequestDto
    {
        /// <summary>
        /// Số lượng khách
        /// </summary>
        public int? NumberOfGuests { get; set; }
        
        /// <summary>
        /// Giá tối thiểu
        /// </summary>
        public decimal? MinPrice { get; set; }
        
        /// <summary>
        /// Giá tối đa
        /// </summary>
        public decimal? MaxPrice { get; set; }
        
        /// <summary>
        /// Loại giường (King, Queen, Twin...)
        /// </summary>
        public string? BedType { get; set; }
        
        /// <summary>
        /// Diện tích tối thiểu
        /// </summary>
        public decimal? MinRoomSize { get; set; }
        
        /// <summary>
        /// Ngày check-in (optional - để kiểm tra availability)
        /// </summary>
        public DateTime? CheckInDate { get; set; }
        
        /// <summary>
        /// Ngày check-out (optional - để kiểm tra availability)
        /// </summary>
        public DateTime? CheckOutDate { get; set; }
        
        /// <summary>
        /// Chỉ hiển thị loại phòng active
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }

    /// <summary>
    /// DTO trả về thông tin RoomType cho customer
    /// </summary>
    public class RoomTypeSearchResultDto
    {
        public int RoomTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePriceNight { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        public bool IsActive { get; set; }
        public List<MediumDto> Images { get; set; } = new List<MediumDto>();
        public List<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
        
        /// <summary>
        /// Số phòng available (nếu có CheckIn/Out date)
        /// </summary>
        public int? AvailableRoomCount { get; set; }
        
        /// <summary>
        /// Tổng số phòng thuộc loại này
        /// </summary>
        public int TotalRoomCount { get; set; }
    }

    // ============= ROOM TYPE STATISTICS & ANALYTICS =============
    /// <summary>
    /// Request thống kê loại phòng với nhiều filter khác nhau
    /// </summary>
    public class RoomTypeStatisticsRequest
    {
        /// <summary>
        /// Loại thống kê: "overview", "most_booked", "by_price", "by_occupancy", "booking_stats"
        /// </summary>
        public string StatisticType { get; set; } = "overview";
        
        /// <summary>
        /// Số lượng top items (cho most_booked)
        /// </summary>
        public int TopCount { get; set; } = 10;
        
        /// <summary>
        /// Giá tối thiểu (cho by_price)
        /// </summary>
        public decimal? MinPrice { get; set; }
        
        /// <summary>
        /// Giá tối đa (cho by_price)
        /// </summary>
        public decimal? MaxPrice { get; set; }
        
        /// <summary>
        /// Số lượng khách tối thiểu (cho by_occupancy)
        /// </summary>
        public int? MinOccupancy { get; set; }
        
        /// <summary>
        /// Số lượng khách tối đa (cho by_occupancy)
        /// </summary>
        public int? MaxOccupancy { get; set; }
        
        /// <summary>
        /// Ngày bắt đầu (cho booking_stats)
        /// </summary>
        public DateTime? FromDate { get; set; }
        
        /// <summary>
        /// Ngày kết thúc (cho booking_stats)
        /// </summary>
        public DateTime? ToDate { get; set; }
        
        /// <summary>
        /// Chỉ lấy loại phòng active
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }

    // ============= ROOM TYPE CRUD (FOR ADMIN) =============
    public class GetRoomTypeListRequest : PagedRequestDto
    {
        public bool? IsActive { get; set; }
    }

    public class AddRoomTypeRequest
    {
        public string TypeName { get; set; } = null!;
        public string TypeCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal BasePriceNight { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateRoomTypeRequest
    {
        public int RoomTypeId { get; set; }
        public string? TypeName { get; set; }
        public string? TypeCode { get; set; }
        public string? Description { get; set; }
        public decimal? BasePriceNight { get; set; }
        public int? MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class RoomTypeWithImagesDto
    {
        public int RoomTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePriceNight { get; set; }
        public int MaxOccupancy { get; set; }
        public decimal? RoomSize { get; set; }
        public int? NumberOfBeds { get; set; }
        public string? BedType { get; set; }
        public bool IsActive { get; set; }
        public List<MediumDto> Images { get; set; } = new List<MediumDto>();
        public int TotalRooms { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ============= ROOM CRUD (FOR ADMIN ONLY) =============
    /// <summary>
    /// Request lấy danh sách phòng cụ thể (chỉ dành cho Admin)
    /// </summary>
    public class GetRoomListRequest : PagedRequestDto
    {
        /// <summary>
        /// Lọc theo loại phòng
        /// </summary>
        public int? RoomTypeId { get; set; }
        
        /// <summary>
        /// Lọc theo trạng thái phòng
        /// </summary>
        public int? StatusId { get; set; }
    }

    public class AddRoomRequest
    {
        public string RoomName { get; set; } = null!;
        public int RoomTypeId { get; set; }
        public int StatusId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateRoomRequest
    {
        public int RoomId { get; set; }
        public string? RoomName { get; set; }
        public int? RoomTypeId { get; set; }
        public int? StatusId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class RoomWithImagesDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public string? RoomTypeName { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? Description { get; set; }
        public List<MediumDto> Images { get; set; } = new List<MediumDto>();
        public List<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
