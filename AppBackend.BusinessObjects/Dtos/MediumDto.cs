namespace AppBackend.BusinessObjects.Dtos
{
    public class MediumDto
    {
        public int? MediaId { get; set; }
        public string FilePath { get; set; } = null!;
        public string? Description { get; set; }
        public string ReferenceTable { get; set; } = null!;
        public string ReferenceKey { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}

