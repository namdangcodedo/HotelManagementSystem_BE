using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Dtos
{
    public class AttendanceDTO
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string? DeviceEmployeeId { get; set; }

        public decimal? OvertimeHours { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public string? Status { get; set; }
        public string? IsApproved { get; set; }

        public string? EmployeeName { get; set; }
    }
}
