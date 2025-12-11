using AppBackend.Services.ApiModels.CustomerModel;
using AppBackend.Services.Services.CustomerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý khách hàng: xem danh sách, chi tiết và khoá/mở khoá tài khoản
    /// </summary>
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomerController : BaseApiController
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Lấy danh sách khách hàng kèm thống kê booking
        /// </summary>
        /// <remarks>
        /// - Hỗ trợ tìm kiếm theo tên/điện thoại/CMND/Email  
        /// - Trả về tổng số booking, số tiền đã chi, ngày booking gần nhất cho mỗi khách
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> GetCustomers([FromQuery] GetCustomerListRequest request)
        {
            var result = await _customerService.GetCustomerListAsync(request);
            return HandleResult(result);
        }

        /// <summary>
        /// Xem chi tiết khách hàng kèm thống kê tham gia hệ thống
        /// </summary>
        /// <param name="customerId">ID khách hàng</param>
        [HttpGet("{customerId}")]
        [Authorize(Roles = "Admin,Manager,Receptionist")]
        public async Task<IActionResult> GetCustomerDetail(int customerId)
        {
            var result = await _customerService.GetCustomerDetailAsync(customerId);
            return HandleResult(result);
        }

        /// <summary>
        /// Khoá/Mở khoá tài khoản khách hàng
        /// </summary>
        /// <param name="customerId">ID khách hàng</param>
        /// <param name="request">Trạng thái khoá</param>
        [HttpPatch("{customerId}/ban")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanCustomer(int customerId, [FromBody] BanCustomerRequest request)
        {
            request.CustomerId = customerId;
            var result = await _customerService.BanCustomerAsync(request);
            return HandleResult(result);
        }
    }
}
