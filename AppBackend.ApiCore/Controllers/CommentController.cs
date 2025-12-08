using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.CommentModel;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Services.AttendanceServices;
using AppBackend.Services.Services.CommentService;
using AppBackend.Services.Services.EmployeeServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing employees
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentController : BaseApiController
    {
        private readonly ICommentService _commentService;
        private readonly string idClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

       
        [HttpGet("Comment")]
        public async Task<IActionResult> GetCommentByPost(GetCommentRequest request)
        {
            var result = await _commentService.GetCommentsByPostId(request);
            return HandleResult(result);
        }

        
        [HttpPost("Comment")]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] PostCommentRequest request)
        {
            var userId = User.FindFirst(idClaim).ToString();
            var result = await _commentService.AddComment(request, int.Parse(userId));
            return HandleResult(result);
        }

        [HttpPut("Comment")]
        [Authorize]
        public async Task<IActionResult> UpdateComent([FromBody] PostCommentRequest request)
        {
            var userId = User.FindFirst(idClaim).ToString();
            var result = await _commentService.UpdateComment(request, int.Parse(userId));
            return HandleResult(result);
        }

    }
}
