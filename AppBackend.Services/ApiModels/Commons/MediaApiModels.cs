namespace AppBackend.Services.ApiModels.Commons
{
    public class MediaCrudDto
    {
        /// <summary>
        /// Database ID if the medium already exists (for keep/remove operations)
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// CRUD action: "add" | "keep" | "remove"
        /// </summary>
        public string CrudKey { get; set; } = string.Empty;

        /// <summary>
        /// Cloudinary provider ID or external reference
        /// </summary>
        public string? ProviderId { get; set; }

        /// <summary>
        /// File URL or path (used when adding new media or updating existing)
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Alt text or description for the media
        /// </summary>
        public string? AltText { get; set; }
    }
}
