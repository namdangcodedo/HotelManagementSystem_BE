using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoomRepo
{
    public class RoomRepository : GenericRepository<Room>, IRoomRepository
    {
        private readonly HotelManagementContext _context;

        public RoomRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Room>> GetByStatusAsync(int statusId)
        {
            return await _context.Rooms.Where(r => r.StatusId == statusId).ToListAsync();
        }


        public async Task<IEnumerable<Room>> getRoomByType(string typeNameStr, int? statusId)
        {
            var rooms = await _context.Rooms.Include(r => r.RoomType).Where(r => typeNameStr.Contains('%' + r.RoomType.TypeName + '%')).ToListAsync();

            if(statusId != null)
            {
                rooms = rooms.Where(r => r.StatusId == statusId).ToList();
            }
            return rooms;
        }
        // Thêm các phương thức đặc thù nếu cần
    }
}

