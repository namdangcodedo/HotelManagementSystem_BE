namespace AppBackend.BusinessObjects.Dtos
{
    public class PagedAmenityRequestDto : PagedRequestDto
    {
        public bool? IsActive { get; set; }
        public string? AmenityType { get; set; } // Common, Premium, VIP, Luxury
    }
}
