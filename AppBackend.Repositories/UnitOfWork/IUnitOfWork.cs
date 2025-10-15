using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using AppBackend.Repositories.Repositories.CustomerRepo;
using System;
using System.Threading.Tasks;
using AppBackend.Repositories.Repositories.MediumRepo;
using AppBackend.Repositories.Repositories.AmenityRepo;

namespace AppBackend.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        IRoleRepository Roles { get; }
        ICommonCodeRepository CommonCodes { get; }
        IRoomRepository Rooms { get; }
        ICustomerRepository Customers { get; }
        IMediumRepository Mediums { get; }
        IAmenityRepository Amenities { get; }
        Task<int> SaveChangesAsync();
    }
}
