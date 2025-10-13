using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Index("Code", Name = "UQ__Semester__357D4CF91CF41FDF", IsUnique = true)]
public partial class Semester
{
    [Key]
    [Column("semester_id")]
    public int SemesterId { get; set; }

    [Column("code")]
    [StringLength(255)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    [Column("year")]
    public int? Year { get; set; }

    [Column("term")]
    [StringLength(255)]
    public string? Term { get; set; }

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [InverseProperty("Semester")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [InverseProperty("Semester")]
    public virtual ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();
}
