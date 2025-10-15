using System;

namespace AppBackend.Services.ApiModels.EmployeeModel
{
    public class UpdateEmployeeRequest
    {
        public int EmployeeId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FullName { get; set; }
        public int? EmployeeTypeId { get; set; }
        public DateOnly? HireDate { get; set; }
        public DateOnly? TerminationDate { get; set; }
    }
}

