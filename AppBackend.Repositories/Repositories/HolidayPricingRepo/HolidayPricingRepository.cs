using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.HolidayPricingRepo
{
    public class HolidayPricingRepository : GenericRepository<HolidayPricing>, IHolidayPricingRepository
    {
        public HolidayPricingRepository(HotelManagementContext context) : base(context)
        {
        }
    }
}

