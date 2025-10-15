namespace AppBackend.BusinessObjects.Dtos
{
    public class AmenityDto
    {
        public int AmenityId { get; set; }
        public string AmenityName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string>? ImageLinks { get; set; }
    }
}
