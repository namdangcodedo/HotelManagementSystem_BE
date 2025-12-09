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

        public async Task<(List<Comment> Comments, int TotalCount)> GetCommentsWithRepliesAsync(
            int? roomTypeId,
            int? parentCommentId,
            bool includeReplies,
            int maxDepth,
            int pageNumber,
            int pageSize,
            bool? isNewest)
        {
            var query = _context.Comments
                .Include(c => c.Account)
                    .ThenInclude(a => a.Customer)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Employee)
                .Include(c => c.RoomType)
                .Where(c => c.Status == "Approved") // Chỉ lấy comment đã duyệt
                .AsQueryable();

            // Filter by RoomTypeId
            if (roomTypeId.HasValue)
            {
                query = query.Where(c => c.RoomTypeId == roomTypeId.Value);
            }

            // Filter: get top-level comments or replies to a specific comment
            if (parentCommentId.HasValue)
            {
                query = query.Where(c => c.ReplyId == parentCommentId.Value);
            }
            else
            {
                // Only get top-level comments (không phải reply)
                query = query.Where(c => c.ReplyId == null);
            }

            // Sorting
            if (isNewest.HasValue && isNewest.Value)
            {
                query = query.OrderByDescending(c => c.CreatedDate).ThenByDescending(c => c.CreatedTime);
            }
            else
            {
                query = query.OrderBy(c => c.CreatedDate).ThenBy(c => c.CreatedTime);
            }

            var totalCount = await query.CountAsync();

            var comments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Include replies if requested
            if (includeReplies && maxDepth > 0)
            {
                foreach (var comment in comments)
                {
                    await LoadRepliesRecursiveAsync(comment, maxDepth);
                }
            }

            return (comments, totalCount);
        }

        private async Task LoadRepliesRecursiveAsync(Comment comment, int depth)
        {
            if (depth <= 0) return;

            var replies = await _context.Comments
                .Include(c => c.Account)
                    .ThenInclude(a => a.Customer)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Employee)
                .Where(c => c.ReplyId == comment.CommentId && c.Status == "Approved") // Chỉ lấy reply đã duyệt
                .OrderBy(c => c.CreatedDate)
                .ThenBy(c => c.CreatedTime)
                .ToListAsync();

            comment.InverseReply = replies;

            if (depth > 1)
            {
                foreach (var reply in replies)
                {
                    await LoadRepliesRecursiveAsync(reply, depth - 1);
                }
            }
        }
    }
}
