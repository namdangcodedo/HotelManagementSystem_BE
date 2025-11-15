using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Employee")]
public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [ForeignKey("Account")]
    public int AccountId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = null!;
    
    [StringLength(100)]
    public string? PhoneNumber { get; set; } = null!;
    
    [Required]
    [ForeignKey("EmployeeType")]
    public int EmployeeTypeId { get; set; }
    public virtual CommonCode EmployeeType { get; set; } = null!;

    [Required]
    public DateOnly HireDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();
    public virtual ICollection<HousekeepingTask> HousekeepingTasks { get; set; } = new List<HousekeepingTask>();
    public virtual ICollection<Salary> Salaries { get; set; } = new List<Salary>();
    public virtual ICollection<PayrollDisbursement> PayrollDisbursements { get; set; } = new List<PayrollDisbursement>();
}
