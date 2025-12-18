using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.EmployeeModel
{
    /// <summary>
    /// Request model để tìm kiếm nhân viên
    /// </summary>
    public class SearchEmployeeRequest : PagedRequestDto
    {
        /// <summary>
        /// Từ khóa tìm kiếm (tìm trên tất cả các trường: FullName, PhoneNumber, Email, Username)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Lọc theo loại nhân viên (optional)
        /// </summary>
        public int? EmployeeTypeId { get; set; }

        /// <summary>
        /// Lọc theo trạng thái hoạt động (true: đang làm việc, false: đã nghỉ việc, null: tất cả)
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Lọc theo trạng thái tài khoản (true: đã khóa, false: đang hoạt động, null: tất cả)
        /// </summary>
        public bool? IsLocked { get; set; }
    }
}
