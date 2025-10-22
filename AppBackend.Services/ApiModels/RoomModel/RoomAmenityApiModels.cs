using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.RoomModel
{
    // ============= ROOM AMENITY CRUD =============
    
    public class AddRoomAmenityRequest
    {
        public int RoomId { get; set; }
        public int AmenityId { get; set; }
    }

    public class UpdateRoomAmenitiesRequest
    {
        public int RoomId { get; set; }
        public List<int> AmenityIds { get; set; } = new List<int>();
    }

    public class RoomAmenityDto
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int AmenityId { get; set; }
        public string AmenityName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string AmenityType { get; set; } = "Common";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
