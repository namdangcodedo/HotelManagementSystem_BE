using System;
using System.Collections.Generic;

namespace AppBackend.BusinessObjects.Dtos
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? IdentityCard { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

