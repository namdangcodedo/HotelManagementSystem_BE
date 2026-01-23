using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.BookingRepo
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        private readonly HotelManagementContext _context;

        public BookingRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả bookings với Include Customer, Account và BookingRooms
        /// Dùng cho search/filter booking với thông tin khách hàng
        /// </summary>
        public async Task<IEnumerable<Booking>> GetAllWithCustomerAndAccountAsync()
        {
            return await _context.Bookings
                .Include(b => b.Customer)
                    .ThenInclude(c => c.Account)
                .Include(b => b.BookingRooms)
                .ToListAsync();
        }
    }
}
