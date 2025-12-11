using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.MediaService
{
    /// <summary>
    /// Service for managing media CRUD operations with smart add/keep/remove logic
    /// </summary>
    public class MediaService : IMediaService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MediaService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Process media CRUD operations for an owner entity.
        /// - Loads existing mediums for the owner
        /// - Applies add/keep/remove operations based on CrudKey
        /// - Maintains and updates DisplayOrder for all items
        /// - Persists changes and returns final ordered list
        /// </summary>
        public async Task<List<Medium>> ProcessMediaCrudAsync(
            IEnumerable<MediaCrudDto>? items,
            string ownerType,
            int ownerId,
            int userId,
            CancellationToken ct = default)
        {
            // Load existing mediums for this owner
            var existing = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == ownerType && m.ReferenceKey == ownerId.ToString())).ToList();

            // If no incoming list -> keep existing as-is (do nothing)
            if (items == null)
            {
                return existing.OrderBy(m => m.DisplayOrder).ToList();
            }

            var itemsList = items.ToList();
            var keepIds = new HashSet<int>();
            int order = 0;

            foreach (var item in itemsList)
            {
                var key = item.CrudKey?.Trim().ToLowerInvariant();

                switch (key)
                {
                    case "add":
                        // Create new medium
                        // Prefer Url if provided, otherwise fallback to ProviderId
                        var filePath = !string.IsNullOrWhiteSpace(item.Url)
                            ? item.Url
                            : (item.ProviderId ?? string.Empty);

                        if (string.IsNullOrWhiteSpace(filePath))
                            continue; // Skip if no valid path/url/providerId

                        var newMedium = new Medium
                        {
                            ReferenceTable = ownerType,
                            ReferenceKey = ownerId.ToString(),
                            FilePath = filePath,
                            Description = item.AltText,
                            DisplayOrder = order++,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId,
                            IsActive = true
                        };
                        await _unitOfWork.Mediums.AddAsync(newMedium);
                        break;

                    case "keep":
                        if (item.Id.HasValue)
                        {
                            var exist = existing.FirstOrDefault(e => e.MediaId == item.Id.Value);
                            if (exist != null)
                            {
                                // Update display order and optional fields
                                exist.DisplayOrder = order++;
                                if (!string.IsNullOrWhiteSpace(item.Url))
                                {
                                    exist.FilePath = item.Url;
                                }
                                if (!string.IsNullOrWhiteSpace(item.AltText))
                                {
                                    exist.Description = item.AltText;
                                }
                                exist.UpdatedAt = DateTime.UtcNow;
                                exist.UpdatedBy = userId;
                                await _unitOfWork.Mediums.UpdateAsync(exist);
                                keepIds.Add(exist.MediaId);
                            }
                        }
                        break;

                    case "remove":
                        if (item.Id.HasValue)
                        {
                            var toDelete = existing.FirstOrDefault(e => e.MediaId == item.Id.Value);
                            if (toDelete != null)
                            {
                                await _unitOfWork.Mediums.DeleteAsync(toDelete);
                            }
                        }
                        break;

                    default:
                        // Unknown key: treat as keep if Id is provided
                        if (item.Id.HasValue)
                        {
                            var e = existing.FirstOrDefault(x => x.MediaId == item.Id.Value);
                            if (e != null)
                            {
                                e.DisplayOrder = order++;
                                e.UpdatedAt = DateTime.UtcNow;
                                e.UpdatedBy = userId;
                                await _unitOfWork.Mediums.UpdateAsync(e);
                                keepIds.Add(e.MediaId);
                            }
                        }
                        break;
                }
            }

            // Persist all changes (add/update/delete)
            await _unitOfWork.SaveChangesAsync();

            // Return final list for this owner ordered by DisplayOrder
            var final = (await _unitOfWork.Mediums.FindAsync(m =>
                m.ReferenceTable == ownerType && m.ReferenceKey == ownerId.ToString()))
                .OrderBy(m => m.DisplayOrder)
                .ToList();

            return final;
        }
    }
}
