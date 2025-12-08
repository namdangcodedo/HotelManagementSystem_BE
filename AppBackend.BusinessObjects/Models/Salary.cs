using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("SalaryRecord")]
public class SalaryRecord
{
    [Key]
    public int SalaryRecordId { get; set; }

    [Required]
    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Required]
    [ForeignKey("Status")]
    public int StatusId { get; set; }
    public virtual CommonCode Status { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
