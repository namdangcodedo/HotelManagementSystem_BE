using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.TransactionRepo
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        private readonly HotelManagementContext _context;

        public TransactionRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Transaction>> GetByBookingIdAsync(int bookingId)
        {
            return await _context.Transactions
                .Where(t => t.BookingId == bookingId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}

