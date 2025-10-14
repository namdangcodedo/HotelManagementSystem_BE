using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.CustomerRepo
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        // Add custom methods for Customer if needed
    }
}

