using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Repositories.Repositories.CommentRepo
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        private readonly HotelManagementContext _context;
        public CommentRepository(HotelManagementContext context) : base(context)
        {
            _context = context;
        }

        public async Task DeleteComment(int commentId)
        {
            var exittingComment = await _context.Comments.FindAsync(commentId);
            if (exittingComment != null)
            {
                _context.Comments.Remove(exittingComment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Comment>> GetCommentsByRoomTypeId(int roomTypeId)
        {
            return await _context.Comments.Where(c => c.RoomTypeId == roomTypeId).ToListAsync();
        }

        public async Task InsertComment(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            _context.SaveChangesAsync();
        }

        public async Task UpdateComment(Comment comment)
        {
            var exittingComment = await _context.Comments.FindAsync(comment.CommentId);
            if (exittingComment != null)
            {
                exittingComment.Content = comment.Content;
                exittingComment.UpdatedAt = DateTime.Now;
                exittingComment.Status = comment.Status;
                exittingComment.Rating = comment.Rating;
                _context.Comments.Update(exittingComment);
                _context.SaveChangesAsync();
            }
        }
    }
}
