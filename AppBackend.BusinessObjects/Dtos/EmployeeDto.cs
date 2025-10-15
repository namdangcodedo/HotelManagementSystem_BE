using System;

namespace AppBackend.BusinessObjects.Dtos
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int EmployeeTypeId { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly HireDate { get; set; }
        public DateOnly? TerminationDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

