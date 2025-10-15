using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.CommonCodeRepo
{
    public interface ICommonCodeRepository : IGenericRepository<CommonCode>
    {
        Task<IEnumerable<CommonCode>> GetByTypeAsync(string codeType);
        // Thêm các phương thức đặc thù nếu cần
    }
}

