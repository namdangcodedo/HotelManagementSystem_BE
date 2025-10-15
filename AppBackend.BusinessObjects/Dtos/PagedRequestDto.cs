namespace AppBackend.BusinessObjects.Dtos
{
    public class PagedRequestDto
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; } = false;
    }
}

