using AppBackend.Services.ApiModels.CommentModel;
using AppBackend.Services.Services.CommentService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing comments
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : BaseApiController
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Lấy danh sách bình luận theo RoomTypeId hoặc theo CommentId cha
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] GetCommentRequest request)
        {
            var result = await _commentService.GetCommentsByRoomTypeId(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Thêm bình luận mới (yêu cầu đăng nhập)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            var result = await _commentService.AddComment(request, accountId);
            return HandleResult(result);
        }

        /// <summary>
        /// Cập nhật bình luận (chỉ chủ sở hữu mới có thể cập nhật)
        /// </summary>
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentRequest request)
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            var result = await _commentService.UpdateComment(request, accountId);
            return HandleResult(result);
        }

        /// <summary>
        /// Ẩn bình luận (yêu cầu role: Receptionist, Manager, Admin)
        /// </summary>
        [HttpPatch("{commentId}/hide")]
        [Authorize(Roles = "Receptionist,Manager,Admin")]
        public async Task<IActionResult> HideComment(int commentId)
        {
            var result = await _commentService.HideComment(commentId);
            return HandleResult(result);
        }
    }
}
