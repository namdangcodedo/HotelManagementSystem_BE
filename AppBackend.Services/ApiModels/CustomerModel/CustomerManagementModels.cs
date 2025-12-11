using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.CustomerModel
{
    public class GetCustomerListRequest : PagedRequestDto
    {
        public bool? IsLocked { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class BanCustomerRequest
    {
        public int CustomerId { get; set; }
        public bool IsLocked { get; set; }
    }

    public class CustomerListItemDto
    {
        public int CustomerId { get; set; }
        public int? AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsLocked { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerDetailResponse
    {
        public CustomerBasicInfo BasicInfo { get; set; } = new CustomerBasicInfo();
        public AccountSnapshot? Account { get; set; }
        public CustomerStatistics Statistics { get; set; } = new CustomerStatistics();
        public List<CustomerBookingBrief> RecentBookings { get; set; } = new List<CustomerBookingBrief>();
    }

    public class CustomerBasicInfo
    {
        public int CustomerId { get; set; }
        public int? AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AccountSnapshot
    {
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CustomerStatistics
    {
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int UpcomingBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public int TotalFeedbacks { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    public class CustomerBookingBrief
    {
        public int BookingId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
