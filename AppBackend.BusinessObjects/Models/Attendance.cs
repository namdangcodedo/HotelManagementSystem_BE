using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Attendance")]
public partial class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    [Required]
    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }

    // New: employee identifier from the attendance device (e.g., fingerprint/terminal id or employee code)
    [StringLength(100)]
    public string? DeviceEmployeeId { get; set; }

    [Required]
    public DateTime CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    [StringLength(255)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    [StringLength(255)]
    public string? Status { get; set; }

    [StringLength(255)]
    public string? IsApproved { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
