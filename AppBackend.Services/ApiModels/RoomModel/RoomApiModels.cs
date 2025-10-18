using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.RoomModel
{
    public class GetRoomListRequest : PagedRequestDto
    {
        public int? RoomTypeId { get; set; }
        public int? StatusId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class AddRoomRequest
    {
        public string RoomNumber { get; set; } = null!;
        public int RoomTypeId { get; set; }
        public decimal BasePriceNight { get; set; }
        public decimal BasePriceHour { get; set; }
        public int StatusId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateRoomRequest
    {
        public int RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public int? RoomTypeId { get; set; }
        public decimal? BasePriceNight { get; set; }
        public decimal? BasePriceHour { get; set; }
        public int? StatusId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class RoomWithImagesDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int RoomTypeId { get; set; }
        public string? RoomTypeName { get; set; }
        public decimal BasePriceNight { get; set; }
        public decimal BasePriceHour { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? Description { get; set; }
        public List<MediumDto> Images { get; set; } = new List<MediumDto>();
        public List<AmenityDto> Amenities { get; set; } = new List<AmenityDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Room Type Models
    public class GetRoomTypeListRequest : PagedRequestDto
    {
        public bool? IsActive { get; set; }
    }

    public class AddRoomTypeRequest
    {
        public string RoomTypeName { get; set; } = null!;
        public string? Description { get; set; }
        public int? MaxOccupancy { get; set; }
        public decimal? BasePrice { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateRoomTypeRequest
    {
        public int RoomTypeId { get; set; }
        public string? RoomTypeName { get; set; }
        public string? Description { get; set; }
        public int? MaxOccupancy { get; set; }
        public decimal? BasePrice { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class RoomTypeWithImagesDto
    {
        public int CodeId { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public List<MediumDto> Images { get; set; } = new List<MediumDto>();
        public int TotalRooms { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
