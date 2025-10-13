using AppBackend.BusinessObjects.Data;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;
using AppBackend.Repositories.Repositories.CommonCodeRepo;
using AppBackend.Repositories.Repositories.RoomRepo;
using System;

namespace AppBackend.Repositories.Repositories.UnitOfWork
{
    public class UnitOfWork : IDisposable
    {
        private readonly HotelManagementContext _context;
        private AccountRepository? _accountRepository;
        private RoleRepository? _roleRepository;
        private CommonCodeRepository? _commonCodeRepository;
        private RoomRepository? _roomRepository;

        public UnitOfWork(HotelManagementContext context)
        {
            _context = context;
        }

        public AccountRepository AccountRepository => _accountRepository ??= new AccountRepository(_context);
        public RoleRepository RoleRepository => _roleRepository ??= new RoleRepository(_context);
        public CommonCodeRepository CommonCodeRepository => _commonCodeRepository ??= new CommonCodeRepository(_context);
        public RoomRepository RoomRepository => _roomRepository ??= new RoomRepository(_context);

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

