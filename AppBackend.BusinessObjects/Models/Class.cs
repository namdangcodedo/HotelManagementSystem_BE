using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Class
{
    [Key]
    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("class_name")]
    [StringLength(255)]
    public string? ClassName { get; set; }

    [Column("instructor_id")]
    public int? InstructorId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("semester_id")]
    public int? SemesterId { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    [ForeignKey("InstructorId")]
    [InverseProperty("Classes")]
    public virtual User? Instructor { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [InverseProperty("Class")]
    public virtual ICollection<RubricWeight> RubricWeights { get; set; } = new List<RubricWeight>();

    [ForeignKey("SemesterId")]
    [InverseProperty("Classes")]
    public virtual Semester? Semester { get; set; }
}
