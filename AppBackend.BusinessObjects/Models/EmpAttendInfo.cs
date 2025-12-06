using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("EmpAttendInfo")]
public partial class EmpAttendInfo
{
    [Key]
    public int AttendInfoId { get; set; }

    [Required]
    [ForeignKey("Employee")]
    public int EmployeeId { get; set; }

    [Required]
    public int Year { get; set; }

    public int? TotalLeaveRequest { get; set; }

    public int? RemainLeaveRequest { get; set; }

    public int? UsedLeaveRequest { get; set; }

    public int? OverLeaveDay { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
