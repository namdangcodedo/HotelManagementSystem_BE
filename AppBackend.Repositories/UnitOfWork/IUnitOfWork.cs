using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using AppBackend.Repositories.Repositories.RoomTypeRepo;
using AppBackend.Repositories.Repositories.CustomerRepo;
using AppBackend.Repositories.Repositories.MediumRepo;
using System.Threading.Tasks;
using AppBackend.Repositories.Repositories.AmenityRepo;
using AppBackend.Repositories.Repositories.EmployeeRepo;
using AppBackend.Repositories.Repositories.RoomAmenityRepo;
using AppBackend.Repositories.Repositories.BookingRepo;
using AppBackend.Repositories.Repositories.BookingRoomRepo;
using AppBackend.Repositories.Repositories.TransactionRepo;
using AppBackend.Repositories.Repositories.HolidayPricingRepo;

namespace AppBackend.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        IRoleRepository Roles { get; }
        ICommonCodeRepository CommonCodes { get; }
        IRoomRepository Rooms { get; }
        IRoomTypeRepository RoomTypes { get; }
        ICustomerRepository Customers { get; }
        IMediumRepository Mediums { get; }
        IAmenityRepository Amenities { get; }
        IEmployeeRepository Employees { get; }
        IRoomAmenityRepository RoomAmenities { get; }
        IBookingRepository Bookings { get; }
        IBookingRoomRepository BookingRooms { get; }
        ITransactionRepository Transactions { get; }
        IHolidayPricingRepository HolidayPricings { get; }
        Task<int> SaveChangesAsync();
    }
}
