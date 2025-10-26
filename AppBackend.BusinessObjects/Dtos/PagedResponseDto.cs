namespace AppBackend.BusinessObjects.Dtos
{
    /// <summary>
    /// Generic paged response model for consistent API pagination across all endpoints
    /// </summary>
    /// <typeparam name="T">The type of items in the paged result</typeparam>
    public class PagedResponseDto<T>
    {
        /// <summary>
        /// List of items in the current page
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
        
        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Current page index (1-based)
        /// </summary>
        public int PageIndex { get; set; }
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }
    }
}

