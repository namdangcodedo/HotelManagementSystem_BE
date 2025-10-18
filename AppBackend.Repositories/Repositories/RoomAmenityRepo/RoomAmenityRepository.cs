using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoomAmenityRepo
{
    public class RoomAmenityRepository : GenericRepository<RoomAmenity>, IRoomAmenityRepository
    {
        private readonly HotelManagementContext _context;

        public RoomAmenityRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomAmenity>> GetAmenitiesByRoomIdAsync(int roomId)
        {
            return await _context.Set<RoomAmenity>()
                .Include(ra => ra.Amenity)
                .Where(ra => ra.RoomId == roomId)
                .ToListAsync();
        }

        public async Task<IEnumerable<RoomAmenity>> GetRoomsByAmenityIdAsync(int amenityId)
        {
            return await _context.Set<RoomAmenity>()
                .Include(ra => ra.Room)
                .Where(ra => ra.AmenityId == amenityId)
                .ToListAsync();
        }

        public async Task<RoomAmenity?> GetByRoomAndAmenityAsync(int roomId, int amenityId)
        {
            return await _context.Set<RoomAmenity>()
                .Include(ra => ra.Amenity)
                .Include(ra => ra.Room)
                .FirstOrDefaultAsync(ra => ra.RoomId == roomId && ra.AmenityId == amenityId);
        }

        public async Task<bool> ExistsAsync(int roomId, int amenityId)
        {
            return await _context.Set<RoomAmenity>()
                .AnyAsync(ra => ra.RoomId == roomId && ra.AmenityId == amenityId);
        }
    }
}
