using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Evaluation
{
    [Key]
    [Column("evaluation_id")]
    public int EvaluationId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("instructor_id")]
    public int? InstructorId { get; set; }

    [Column("total_score", TypeName = "decimal(10, 2)")]
    public decimal? TotalScore { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("evaluated_at")]
    [Precision(0)]
    public DateTime? EvaluatedAt { get; set; }

    [InverseProperty("Evaluation")]
    public virtual ICollection<EvaluationDetail> EvaluationDetails { get; set; } = new List<EvaluationDetail>();

    [ForeignKey("InstructorId")]
    [InverseProperty("Evaluations")]
    public virtual User? Instructor { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("Evaluations")]
    public virtual Project? Project { get; set; }
}
