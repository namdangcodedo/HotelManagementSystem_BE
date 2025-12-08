using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.CommentRepo
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        public Task<List<Comment>> GetCommentsByRoomTypeId(int roomTypeId);
        public Task InsertComment(Comment comment);
        public Task UpdateComment(Comment comment);
        public Task DeleteComment(int commentId);

    }
}
