using System;

namespace AppBackend.BusinessObjects.Dtos
{
    public class SalaryInfoDto
    {
        public int SalaryInfoId { get; set; }
        public int EmployeeId { get; set; }

        // optional convenience field populated by mappings
        public string? EmployeeName { get; set; }

        public int Year { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal? Bonus { get; set; }
        public decimal? Allowance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}