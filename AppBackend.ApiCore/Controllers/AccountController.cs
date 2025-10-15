using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.ApiModels;
using AppBackend.Services.Services.AccountServices;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// APIs for managing user profile (View Profile, Edit Profile, Account Summary)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// View customer profile
        /// </summary>
        /// <returns>Customer profile details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Profile not found</response>
        [HttpGet("customer-profile")]
        public async Task<IActionResult> ViewCustomerProfile()
        {
            var result = await _accountService.GetCustomerProfileAsync(CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Edit customer profile
        /// </summary>
        /// <param name="request">Profile data to update</param>
        /// <returns>Updated customer profile details</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPut("customer-profile")]
        public async Task<IActionResult> EditCustomerProfile([FromBody] EditCustomerProfileRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationError("Dữ liệu không hợp lệ");

            request.AccountId = CurrentUserId;
            var result = await _accountService.EditCustomerProfileAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Xem tổng quan tài khoản (Summary)
        /// </summary>
        /// <param name="accountId">ID tài khoản cần xem (tùy chọn, mặc định là tài khoản hiện tại)</param>
        /// <returns>Thông tin tổng quan tài khoản bao gồm: roles, thông tin chi tiết (Customer/Employee), và thống kê (nếu Admin xem)</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        /// <response code="403">Không có quyền xem tài khoản này</response>
        [HttpGet("summary")]
        public async Task<IActionResult> ViewAccountSummary([FromQuery] int? accountId = null)
        {
            int targetAccountId = accountId ?? CurrentUserId;
            
            // Kiểm tra quyền: chỉ được xem summary của chính mình, trừ khi là Admin
            if (!IsAdmin && targetAccountId != CurrentUserId)
            {
                return StatusCode(403, new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    StatusCode = 403,
                    Message = "Bạn không có quyền xem tài khoản này"
                });
            }
            
            var result = await _accountService.GetAccountSummaryAsync(targetAccountId, CurrentUserId);
            return HandleResult(result);
        }

        /// <summary>
        /// Xem tổng quan tài khoản theo ID (Admin only)
        /// </summary>
        /// <param name="id">ID tài khoản cần xem</param>
        /// <returns>Thông tin tổng quan tài khoản với đầy đủ statistics</returns>
        /// <response code="200">Lấy thông tin thành công</response>
        /// <response code="404">Không tìm thấy tài khoản</response>
        [HttpGet("summary/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewAccountSummaryById(int id)
        {
            var result = await _accountService.GetAccountSummaryAsync(id, CurrentUserId);
            return HandleResult(result);
        }
    }
}
