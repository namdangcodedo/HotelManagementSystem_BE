using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Hall_of_Fame")]
[Index("ProjectId", "SemesterId", Name = "uq_project_semester", IsUnique = true)]
public partial class HallOfFame
{
    [Key]
    [Column("hof_id")]
    public int HofId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("nominated_by")]
    public int? NominatedBy { get; set; }

    [Column("nominated_at")]
    [Precision(0)]
    public DateTime? NominatedAt { get; set; }

    [Column("semester_id")]
    public int? SemesterId { get; set; }

    [Column("rank")]
    public int? Rank { get; set; }

    [Column("note")]
    [StringLength(255)]
    public string? Note { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("HallOfFames")]
    public virtual Project? Project { get; set; }

    [ForeignKey("SemesterId")]
    [InverseProperty("HallOfFames")]
    public virtual Semester? Semester { get; set; }
}
