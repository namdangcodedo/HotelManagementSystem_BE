using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoomRepo
{
    public interface IRoomRepository : IGenericRepository<Room>
    {
        Task<IEnumerable<Room>> GetByStatusAsync(int statusId);

        Task<IEnumerable<Room>> getRoomByType(string typeNameStr, int? statusId);
        // Thêm các phương thức đặc thù nếu cần
    }
}

