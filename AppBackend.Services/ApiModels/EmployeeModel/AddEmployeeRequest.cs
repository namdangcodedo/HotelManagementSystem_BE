using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.EmployeeModel
{
    public class AddEmployeeRequest
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Loại nhân viên là bắt buộc")]
        public int EmployeeTypeId { get; set; }

        [Required(ErrorMessage = "Ngày thuê là bắt buộc")]
        public DateOnly HireDate { get; set; }
    }
}

