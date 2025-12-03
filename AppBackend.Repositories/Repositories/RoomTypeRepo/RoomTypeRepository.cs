using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.RoomTypeRepo
{
    public class RoomTypeRepository : GenericRepository<RoomType>, IRoomTypeRepository
    {
        private readonly HotelManagementContext _context;

        public RoomTypeRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }
        
        // Thêm các phương thức đặc thù nếu cần

        public async Task<List<RoomType>> getRoomTypeWithRoom()
        {
            return _context.RoomTypes.Include(rt => rt.Rooms).ToList();
        }
    }
}
