using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.UserRepo
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}