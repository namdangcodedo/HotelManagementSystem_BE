using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.RoomModel
{
    public class AddRoomAmenityRequest
    {
        public int RoomId { get; set; }
        public int AmenityId { get; set; }
    }

    public class AddMultipleRoomAmenitiesRequest
    {
        public int RoomId { get; set; }
        public List<int> AmenityIds { get; set; } = new List<int>();
    }

    public class SyncRoomAmenitiesRequest
    {
        public int RoomId { get; set; }
        public List<int> AmenityIds { get; set; } = new List<int>();
    }

    public class RemoveRoomAmenityRequest
    {
        public int RoomId { get; set; }
        public int AmenityId { get; set; }
    }

    public class RoomAmenityDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int AmenityId { get; set; }
        public string AmenityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AmenityType { get; set; } = "Common";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AmenityWithSelectionDto
    {
        public int AmenityId { get; set; }
        public string AmenityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AmenityType { get; set; } = "Common";
        public bool IsActive { get; set; }
        public bool IsSelected { get; set; }
        public List<string>? ImageLinks { get; set; }
    }

    public class RoomAmenitiesWithSelectionDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public List<AmenityWithSelectionDto> Amenities { get; set; } = new List<AmenityWithSelectionDto>();
    }

    public class RoomWithAmenitiesDto
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
        public List<AmenityDto> CommonAmenities { get; set; } = new List<AmenityDto>();
        public List<AmenityDto> AdditionalAmenities { get; set; } = new List<AmenityDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
