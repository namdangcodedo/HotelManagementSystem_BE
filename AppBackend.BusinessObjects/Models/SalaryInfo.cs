using Microsoft.AspNetCore.Http.HttpResults;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Models
{
    [Table("SalaryInfo")]
    public class SalaryInfo
    {
        [Key]
        public int SalaryInfoId { get; set; }
        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        public decimal BaseSalary { get; set; }
        public decimal? YearBonus { get; set; }
        public decimal? Allowance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public virtual Employee Employee { get; set; } = null!;

    }
}
