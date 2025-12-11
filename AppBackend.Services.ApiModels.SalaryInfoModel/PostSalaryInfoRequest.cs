using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.SalaryInfoModel
{
    public class PostSalaryInfoRequest
    {
        // if provided, treated as update; otherwise create
        public int? SalaryInfoId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public decimal BaseSalary { get; set; }

        public decimal? Bonus { get; set; }
        public decimal? Allowance { get; set; }
    }
}