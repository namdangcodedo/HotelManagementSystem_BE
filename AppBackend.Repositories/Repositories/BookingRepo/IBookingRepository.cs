using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.BookingRepo
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetByCustomerIdAsync(int customerId);
        
        /// <summary>
        /// Lấy tất cả bookings với Include Customer, Account và BookingRooms
        /// Dùng cho search/filter booking với thông tin khách hàng
        /// </summary>
        Task<IEnumerable<Booking>> GetAllWithCustomerAndAccountAsync();
    }
}
