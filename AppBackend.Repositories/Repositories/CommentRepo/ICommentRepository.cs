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
        Task<(List<Comment> Comments, int TotalCount)> GetCommentsWithRepliesAsync(
            int? roomTypeId, 
            int? parentCommentId,
            bool includeReplies,
            int maxDepth,
            int pageNumber,
            int pageSize,
            bool? isNewest);
        public Task InsertComment(Comment comment);
        public Task UpdateComment(Comment comment);
        public Task DeleteComment(int commentId);

    }
}
