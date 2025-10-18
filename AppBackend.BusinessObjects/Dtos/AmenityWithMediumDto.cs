namespace AppBackend.BusinessObjects.Dtos
{
    public class AmenityWithMediumDto
    {
        public int AmenityId { get; set; }
        public string AmenityName { get; set; } = null!;
        public string? Description { get; set; }
        public string AmenityType { get; set; } = "Common";
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public List<string> Images { get; set; } = new();
    }
}
