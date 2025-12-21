using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.SalaryModel
{
    public class CalculateSalaryRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        // Optional overrides
        public decimal? StandardMonthlyHours { get; set; } // default 208
        public decimal? OvertimeMultiplier { get; set; } // default 1.5
    }
}