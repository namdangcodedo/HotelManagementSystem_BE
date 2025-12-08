using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.BookingRoomRepo
{
    public class BookingRoomRepository : GenericRepository<BookingRoom>, IBookingRoomRepository
    {
        private readonly HotelManagementContext _context;

        public BookingRoomRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookingRoom>> GetByBookingIdAsync(int bookingId)
        {
            return await _context.BookingRooms
                .Where(br => br.BookingId == bookingId)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookingRoom>> GetByRoomIdAsync(int roomId)
        {
            return await _context.BookingRooms
                .Where(br => br.RoomId == roomId)
                .OrderByDescending(br => br.CheckInDate)
                .ToListAsync();
        }
    }
}

