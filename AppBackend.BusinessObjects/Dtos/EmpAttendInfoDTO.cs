using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Dtos;

public partial class EmpAttendInfoDTO
{
    public int AttendInfoId { get; set; }

    public int EmployeeId { get; set; }

    public int Year { get; set; }

    public int? TotalLeaveRequest { get; set; }

    public int? RemainLeaveRequest { get; set; }

    public int? UsedLeaveRequest { get; set; }

    public int? OverLeaveDay { get; set; }

}
