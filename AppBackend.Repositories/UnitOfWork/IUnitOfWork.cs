using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using System;
using System.Threading.Tasks;

namespace AppBackend.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        IRoleRepository Roles { get; }
        ICommonCodeRepository CommonCodes { get; }
        IRoomRepository Rooms { get; }
        Task<int> SaveChangesAsync();
    }
}

