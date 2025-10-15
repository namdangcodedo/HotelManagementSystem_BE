using AppBackend.BusinessObjects.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.AmenityRepo
{
    public interface IAmenityRepository
    {
        Task<Amenity?> GetByIdAsync(int id);
        Task<List<Amenity>> GetAllAsync(bool? isActive = null);
        Task AddAsync(Amenity amenity);
        Task UpdateAsync(Amenity amenity);
        Task DeleteAsync(int id);
        Task<(IEnumerable<Amenity> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, bool? isActive = null, string? search = null, string? sortBy = null, bool sortDesc = false);
    }
}
