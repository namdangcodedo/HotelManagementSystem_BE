using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.RoomAmenityRepo
{
    public interface IRoomAmenityRepository : IGenericRepository<RoomAmenity>
    {
        Task<IEnumerable<RoomAmenity>> GetAmenitiesByRoomIdAsync(int roomId);
        Task<IEnumerable<RoomAmenity>> GetRoomsByAmenityIdAsync(int amenityId);
        Task<RoomAmenity?> GetByRoomAndAmenityAsync(int roomId, int amenityId);
        Task<bool> ExistsAsync(int roomId, int amenityId);
    }
}

