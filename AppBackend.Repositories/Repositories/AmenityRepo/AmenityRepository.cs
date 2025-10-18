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
        public async Task<List<Amenity>> GetByTypeAsync(string amenityType, bool? isActive = null)
        {
            var query = _context.Amenities.AsQueryable();
            
            query = query.Where(a => a.AmenityType == amenityType);
            
            if (isActive.HasValue)
                query = query.Where(a => a.IsActive == isActive.Value);
            
            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<Amenity> Items, int TotalCount)> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            bool? isActive = null, 
            string? search = null, 
            string? sortBy = null, 
            bool sortDesc = false,
            string? amenityType = null)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Amenities.AsQueryable();
            
            // Lọc theo trạng thái
            if (isActive.HasValue)
                query = query.Where(a => a.IsActive == isActive.Value);
            
            // Tìm kiếm theo tên hoặc mô tả
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.AmenityName.Contains(search) || 
                                        (a.Description != null && a.Description.Contains(search)));
            
            // Lọc theo loại tiện ích
            if (!string.IsNullOrWhiteSpace(amenityType))
                query = query.Where(a => a.AmenityType == amenityType);
            
            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (sortBy == "AmenityName")
                    query = sortDesc ? query.OrderByDescending(a => a.AmenityName) : query.OrderBy(a => a.AmenityName);
                else if (sortBy == "AmenityType")
                    query = sortDesc ? query.OrderByDescending(a => a.AmenityType) : query.OrderBy(a => a.AmenityType);
                else if (sortBy == "CreatedAt")
                    query = sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(a => a.CreatedAt);
            }
            
            var totalCount = await query.CountAsync();
            
            // Tính lại pageIndex nếu vượt quá số trang thực tế
            var maxPageIndex = totalCount > 0 ? (totalCount - 1) / pageSize : 0;
            if (pageIndex > maxPageIndex) pageIndex = maxPageIndex;
            
            var items = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }
    }
}
