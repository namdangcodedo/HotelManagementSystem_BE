using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.ApiModels.SalaryModel
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

        public decimal? YearBonus { get; set; }
        public decimal? Allowance { get; set; }
    }
}
