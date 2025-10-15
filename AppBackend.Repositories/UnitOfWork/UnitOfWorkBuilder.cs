using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.AccountRepo;
using AppBackend.Repositories.Repositories.RoleRepo;

namespace AppBackend.Repositories.UnitOfWork
{
    public class UnitOfWorkBuilder
    {
        private HotelManagementContext _context;

        public UnitOfWorkBuilder WithContext(HotelManagementContext context)
        {
            _context = context;
            return this;
        }

        public UnitOfWorkBuilder WithAccountRepository()
        {
            return this;
        }

        public UnitOfWorkBuilder WithRoleRepository()
        {
            return this;
        }

        public UnitOfWork Build()
        {
            return new UnitOfWork(_context);
        }
    }
}

