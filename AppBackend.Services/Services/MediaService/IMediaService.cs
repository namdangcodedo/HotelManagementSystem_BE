using AppBackend.BusinessObjects.Models;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MediaService
{
    /// <summary>
    /// Service for managing media (image) CRUD operations with support for add/keep/remove actions
    /// </summary>
    public interface IMediaService
    {
        /// <summary>
        /// Process media CRUD operations for an owner (RoomType/Room/Amenity).
        /// Handles add, keep, and remove operations while maintaining display order.
        /// </summary>
        /// <param name="items">List of media items with CRUD actions</param>
        /// <param name="ownerType">Type of owner: "RoomType", "Room", "Amenity", etc.</param>
        /// <param name="ownerId">ID of the owner entity</param>
        /// <param name="userId">ID of the user performing the action</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Final list of Medium entities ordered by DisplayOrder</returns>
        Task<List<Medium>> ProcessMediaCrudAsync(
            IEnumerable<MediaCrudDto>? items,
            string ownerType,
            int ownerId,
            int userId,
            CancellationToken ct = default);
    }
}
