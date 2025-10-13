using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Class_Enrollments")]
[Index("ClassId", "StudentId", Name = "uq_class_student", IsUnique = true)]
public partial class ClassEnrollment
{
    [Key]
    [Column("enrollment_id")]
    public int EnrollmentId { get; set; }

    [Column("class_id")]
    public int? ClassId { get; set; }

    [Column("student_id")]
    public int? StudentId { get; set; }

    [Column("enrolled_at")]
    [Precision(0)]
    public DateTime? EnrolledAt { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("ClassEnrollments")]
    public virtual Class? Class { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("ClassEnrollments")]
    public virtual User? Student { get; set; }
}
