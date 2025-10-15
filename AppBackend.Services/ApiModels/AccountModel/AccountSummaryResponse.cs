namespace AppBackend.Services.ApiModels.AccountModel
{
    public class AccountSummaryResponse
    {
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string AccountType { get; set; } = string.Empty; // "Customer" or "Employee"
        public object? ProfileDetails { get; set; } // CustomerDto hoặc EmployeeDto
        public AccountStatistics? Statistics { get; set; } // Chỉ có khi Admin xem
    }

    public class AccountStatistics
    {
        // Statistics cho Customer
        public int? TotalBookings { get; set; }
        public int? CompletedBookings { get; set; }
        public int? CancelledBookings { get; set; }
        public decimal? TotalSpent { get; set; }
        public int? TotalFeedbacks { get; set; }
        
        // Statistics cho Employee
        public int? TotalTasksAssigned { get; set; }
        public int? CompletedTasks { get; set; }
        public int? PendingTasks { get; set; }
        public int? TotalAttendance { get; set; }
        public decimal? TotalSalaryPaid { get; set; }
        public int? WorkingDays { get; set; }
        
        // General statistics
        public int? TotalNotifications { get; set; }
        public int? UnreadNotifications { get; set; }
    }

    public class CustomerDetailResponse
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class EmployeeDetailResponse
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int EmployeeTypeId { get; set; }
        public string? EmployeeTypeName { get; set; }
        public DateOnly HireDate { get; set; }
        public DateOnly? TerminationDate { get; set; }
        public bool IsActive => TerminationDate == null;
    }
}

