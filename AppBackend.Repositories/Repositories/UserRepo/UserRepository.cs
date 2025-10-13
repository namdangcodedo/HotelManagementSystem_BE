using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.UserRepo
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly IOTShowroomContext _context;

        public UserRepository(IOTShowroomContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)   
                .FirstOrDefaultAsync(u => u.Email == email);        }
    }
}