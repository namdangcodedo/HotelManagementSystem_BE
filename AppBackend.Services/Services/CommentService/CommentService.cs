using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Enums;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CommentModel;
using AppBackend.Services.Helpers;
using AppBackend.Services.Services.AI;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.CommentService
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICommentModerationService _moderationService;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            IUnitOfWork unitOfWork, 
            IMapper mapper,
            ICommentModerationService moderationService,
            ILogger<CommentService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _moderationService = moderationService;
            _logger = logger;
        }

        public async Task<ResultModel> AddComment(AddCommentRequest request, int accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy tài khoản",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate RoomType exists
            var roomType = await _unitOfWork.RoomTypes.GetByIdAsync(request.RoomTypeId);
            if (roomType == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy loại phòng",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Validate ReplyId if provided
            if (request.ReplyId.HasValue)
            {
                var parentComment = await _unitOfWork.Comments.GetByIdAsync(request.ReplyId.Value);
                if (parentComment == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = "Không tìm thấy bình luận cha",
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }
            }

            // === KIỂM DUYỆT BÌNH LUẬN BẰNG GEMINI AI ===
            _logger.LogInformation("Analyzing comment with AI moderation service...");
            var moderationResult = await _moderationService.AnalyzeCommentAsync(request.Content, request.Rating);

            var now = DateTime.UtcNow;
            var comment = new Comment
            {
                RoomTypeId = request.RoomTypeId,
                ReplyId = request.ReplyId,
                AccountId = accountId,
                Content = request.Content,
                Rating = request.Rating,
                CreatedDate = now.Date,
                CreatedTime = now,
                Status = moderationResult.Status, // Sử dụng status từ AI moderation
                UpdatedAt = now
            };

            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            // Trả về message khác nhau tuỳ vào kết quả kiểm duyệt
            string message;
            if (moderationResult.Status == "Approved")
            {
                message = "Thêm bình luận thành công";
            }
            else if (moderationResult.Status == "Rejected")
            {
                message = "Bình luận bị từ chối do vi phạm quy tắc cộng đồng";
            }
            else // Pending
            {
                message = "Bình luận đang chờ kiểm duyệt";
            }

            _logger.LogInformation("Comment {CommentId} created with status: {Status}. Reason: {Reason}", 
                comment.CommentId, moderationResult.Status, moderationResult.Reason);

            return new ResultModel
            {
                IsSuccess = moderationResult.Status != "Rejected", // Rejected = false
                ResponseCode = moderationResult.Status == "Rejected" ? "REJECTED" : CommonMessageConstants.SUCCESS,
                Message = message,
                Data = moderationResult.Status == "Rejected" ? null : new
                {
                    commentId = comment.CommentId,
                    status = moderationResult.Status
                },
                StatusCode = moderationResult.Status == "Rejected" ? StatusCodes.Status403Forbidden : StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> UpdateComment(UpdateCommentRequest request, int accountId)
        {
            var existingComment = await _unitOfWork.Comments.GetByIdAsync(request.CommentId);
            if (existingComment == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy bình luận",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check if user owns the comment
            if (existingComment.AccountId != accountId)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.UNAUTHORIZED,
                    Message = "Bạn không có quyền chỉnh sửa bình luận này",
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            existingComment.Content = request.Content;
            existingComment.Rating = request.Rating;
            existingComment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Comments.UpdateAsync(existingComment);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật bình luận thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCommentsByRoomTypeId(GetCommentRequest request)
        {
            if (!request.RoomTypeId.HasValue && !request.ParentCommentId.HasValue)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.BAD_REQUEST,
                    Message = "RoomTypeId hoặc ParentCommentId là bắt buộc",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var pageNumber = request.PageIndex;
            var pageSize = request.PageSize;

            var (comments, totalCount) = await _unitOfWork.Comments.GetCommentsWithRepliesAsync(
                request.RoomTypeId,
                request.ParentCommentId,
                request.IncludeReplies,
                request.MaxReplyDepth,
                pageNumber,
                pageSize,
                request.IsNewest
            );

            var commentDtos = _mapper.Map<List<CommentDTO>>(comments);

            var result = new
            {
                Comments = commentDtos,
                TotalCount = totalCount,
                PageIndex = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Lấy danh sách bình luận thành công",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> HideComment(int commentId)
        {
            var existingComment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (existingComment == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Không tìm thấy bình luận",
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            existingComment.Status = "Hidden";
            existingComment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Comments.UpdateAsync(existingComment);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Ẩn bình luận thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
