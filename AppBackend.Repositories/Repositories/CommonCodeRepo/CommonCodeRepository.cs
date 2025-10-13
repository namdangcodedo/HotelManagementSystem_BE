using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.CommonCodeRepo
{
    public class CommonCodeRepository : GenericRepository<CommonCode>, ICommonCodeRepository
    {
        private readonly HotelManagementContext _context;

        public CommonCodeRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CommonCode>> GetByTypeAsync(string codeType)
        {
            return await _context.CommonCodes.Where(c => c.CodeType == codeType).ToListAsync();
        }
        // Thêm các phương thức đặc thù nếu cần
    }
}

