using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.BookingRoomRepo
{
    public interface IBookingRoomRepository : IGenericRepository<BookingRoom>
    {
        Task<IEnumerable<BookingRoom>> GetByBookingIdAsync(int bookingId);
        Task<IEnumerable<BookingRoom>> GetByRoomIdAsync(int roomId);
    }
}
