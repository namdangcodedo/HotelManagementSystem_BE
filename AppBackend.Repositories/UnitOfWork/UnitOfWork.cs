using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using System.Threading.Tasks;

namespace AppBackend.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HotelManagementContext _context;
        private IAccountRepository? _accountRepository;
        private IRoleRepository? _roleRepository;
        private ICommonCodeRepository? _commonCodeRepository;
        private IRoomRepository? _roomRepository;

        public UnitOfWork(HotelManagementContext context)
        {
            _context = context;
        }

        public IAccountRepository Accounts => _accountRepository ??= new AccountRepository(_context);
        public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);
        public ICommonCodeRepository CommonCodes => _commonCodeRepository ??= new CommonCodeRepository(_context);
        public IRoomRepository Rooms => _roomRepository ??= new RoomRepository(_context);

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
