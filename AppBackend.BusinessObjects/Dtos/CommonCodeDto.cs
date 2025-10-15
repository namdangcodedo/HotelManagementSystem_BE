namespace AppBackend.BusinessObjects.Dtos
{
    public class CommonCodeDto
    {
        public int CodeId { get; set; }
        public string CodeType { get; set; } = string.Empty;
        public string CodeValue { get; set; } = string.Empty;
        public string CodeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

