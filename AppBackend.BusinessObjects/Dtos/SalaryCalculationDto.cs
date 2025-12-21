using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Dtos
{
    public class SalaryCalculationDto
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        // hours
        public decimal TotalWorkHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }

        // pay
        public decimal BaseSalary { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal BasePay { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal TotalPay { get; set; }
    }
}
