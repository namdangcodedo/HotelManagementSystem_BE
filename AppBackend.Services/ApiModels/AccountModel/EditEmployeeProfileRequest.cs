using System;

namespace AppBackend.Services.ApiModels
{
    public class EditEmployeeProfileRequest
    {
        public int AccountId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FullName { get; set; }
        public int? EmployeeTypeId { get; set; }
        public DateOnly? HireDate { get; set; }
        public DateOnly? TerminationDate { get; set; }
    }
}

