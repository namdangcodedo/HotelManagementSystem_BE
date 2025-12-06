using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using AppBackend.Repositories.Repositories.RoomTypeRepo;
using AppBackend.Repositories.Repositories.CustomerRepo;
using AppBackend.Repositories.Repositories.MediumRepo;
using AppBackend.Repositories.Repositories.AmenityRepo;
using AppBackend.Repositories.Repositories.EmployeeRepo;
using AppBackend.Repositories.Repositories.RoomAmenityRepo;
using AppBackend.Repositories.Repositories.BookingRepo;
using AppBackend.Repositories.Repositories.BookingRoomRepo;
using AppBackend.Repositories.Repositories.TransactionRepo;
using AppBackend.Repositories.Repositories.HolidayPricingRepo;
using AppBackend.Repositories.Repositories.BankConfigRepo;
using AppBackend.Repositories.Repositories.AttendanceRepo;

namespace AppBackend.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HotelManagementContext _context;
        private IAccountRepository? _accountRepository;
        private IRoleRepository? _roleRepository;
        private ICommonCodeRepository? _commonCodeRepository;
        private IRoomRepository? _roomRepository;
        private IRoomTypeRepository? _roomTypeRepository;
        private ICustomerRepository? _customerRepository;
        private IMediumRepository? _mediumRepository;
        private IAmenityRepository? _amenityRepository;
        private IEmployeeRepository? _employeeRepository;
        private IRoomAmenityRepository? _roomAmenityRepository;
        private IBookingRepository? _bookingRepository;
        private IBookingRoomRepository? _bookingRoomRepository;
        private ITransactionRepository? _transactionRepository;
        private IHolidayPricingRepository? _holidayPricingRepository;
        private IBankConfigRepository? _bankConfigRepository;
        private IAttendenceRepository? _attendenceRepository;

        public UnitOfWork(HotelManagementContext context)
        {
            _context = context;
        }

        public IAccountRepository Accounts => _accountRepository ??= new AccountRepository(_context);
        public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
        public ICommonCodeRepository CommonCodes => _commonCodeRepository ??= new CommonCodeRepository(_context);
        public IRoomRepository Rooms => _roomRepository ??= new RoomRepository(_context);
        public IRoomTypeRepository RoomTypes => _roomTypeRepository ??= new RoomTypeRepository(_context);
        public ICustomerRepository Customers => _customerRepository ??= new CustomerRepository(_context);
        public IMediumRepository Mediums => _mediumRepository ??= new MediumRepository(_context);
        public IAmenityRepository Amenities => _amenityRepository ??= new AmenityRepository(_context);
        public IEmployeeRepository Employees => _employeeRepository ??= new EmployeeRepository(_context);
        public IRoomAmenityRepository RoomAmenities => _roomAmenityRepository ??= new RoomAmenityRepository(_context);
        public IBookingRepository Bookings => _bookingRepository ??= new BookingRepository(_context);
        public IBookingRoomRepository BookingRooms => _bookingRoomRepository ??= new BookingRoomRepository(_context);
        public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_context);
        public IHolidayPricingRepository HolidayPricings => _holidayPricingRepository ??= new HolidayPricingRepository(_context);
        public IBankConfigRepository BankConfigs => _bankConfigRepository ??= new BankConfigRepository(_context);
        public IAttendenceRepository AttendenceRepository => _attendenceRepository ??= new AttendanceRepository(_context);  

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
