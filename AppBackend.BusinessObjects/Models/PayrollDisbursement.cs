using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("PayrollDisbursement")]
public class PayrollDisbursement
{
    [Key]
    public int PayrollDisbursementId { get; set; }

    [Required]
    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }

    [Required]
    public int PayrollMonth { get; set; }

    [Required]
    public int PayrollYear { get; set; }

    // Base salary snapshot at payroll time
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    // Total calculated salary for the payroll (including allowances, overtime, shift adjustments)
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Amount actually disbursed
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DisbursedAmount { get; set; }

    // Status (e.g., Pending, Approved, Disbursed) stored as CommonCode
    [Required]
    [ForeignKey("Status")]
    public int StatusId { get; set; }
    public virtual CommonCode Status { get; set; } = null!;

    public DateTime? DisbursedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}

