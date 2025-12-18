namespace AppBackend.BusinessObjects.Dtos
{
    /// <summary>
    /// DTO cho kết quả tìm kiếm nhân viên (bao gồm thông tin Account)
    /// </summary>
    public class EmployeeSearchResultDto
    {
        public int EmployeeId { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int EmployeeTypeId { get; set; }
        public string EmployeeTypeName { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }
        public DateOnly? TerminationDate { get; set; }
        public decimal BaseSalary { get; set; }

        // Thông tin từ Account
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
