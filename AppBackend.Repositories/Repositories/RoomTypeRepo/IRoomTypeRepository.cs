using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.RoomTypeRepo
{
    public interface IRoomTypeRepository : IGenericRepository<RoomType>
    {
        Task<List<RoomType>> getRoomTypeWithRoom();
    }
}

