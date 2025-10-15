using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.MediumRepo
{
    public class MediumRepository : GenericRepository<Medium>, IMediumRepository
    {
        public MediumRepository(HotelManagementContext context) : base(context)
        {
        }
        // Add custom methods for Medium if needed
    }
}

