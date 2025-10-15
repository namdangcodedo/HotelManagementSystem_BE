using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.AmenityRepo;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace AppBackend.Repositories.Repositories.AmenityRepo
{
    public class AmenityRepository : IAmenityRepository
    {
        private readonly HotelManagementContext _context;
        public AmenityRepository(HotelManagementContext context)
        {
            _context = context;
        }
        public async Task<Amenity?> GetByIdAsync(int id)
        {
            return await _context.Amenities.FindAsync(id);
        }
        public async Task<List<Amenity>> GetAllAsync(bool? isActive = null)
        {
            var query = _context.Amenities.AsQueryable();
            if (isActive.HasValue)
                query = query.Where(a => a.IsActive == isActive.Value);
            return await query.ToListAsync();
        }
        public async Task AddAsync(Amenity amenity)
        {
            await _context.Amenities.AddAsync(amenity);
        }
        public async Task UpdateAsync(Amenity amenity)
        {
            _context.Amenities.Update(amenity);
        }
        public async Task DeleteAsync(int id)
        {
            var amenity = await _context.Amenities.FindAsync(id);
            if (amenity != null)
                _context.Amenities.Remove(amenity);
        }
        public async Task<(IEnumerable<Amenity> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, bool? isActive = null, string? search = null, string? sortBy = null, bool sortDesc = false)
        {
            var query = _context.Amenities.AsQueryable();
            if (isActive.HasValue)
                query = query.Where(a => a.IsActive == isActive.Value);
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.AmenityName.Contains(search));
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (sortBy == "AmenityName")
                    query = sortDesc ? query.OrderByDescending(a => a.AmenityName) : query.OrderBy(a => a.AmenityName);
                else if (sortBy == "Price")
                    query = sortDesc ? query.OrderByDescending(a => a.Price) : query.OrderBy(a => a.Price);
                // Add more sort options if needed
            }
            else
            {
                query = query.OrderBy(a => a.AmenityId);
            }
            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}
