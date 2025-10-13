using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Evaluation_Details")]
[Index("EvaluationId", "RubricId", Name = "uq_eval_rubric", IsUnique = true)]
public partial class EvaluationDetail
{
    [Key]
    [Column("detail_id")]
    public int DetailId { get; set; }

    [Column("evaluation_id")]
    public int? EvaluationId { get; set; }

    [Column("rubric_id")]
    public int? RubricId { get; set; }

    [Column("score", TypeName = "decimal(10, 2)")]
    public decimal? Score { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [ForeignKey("EvaluationId")]
    [InverseProperty("EvaluationDetails")]
    public virtual Evaluation? Evaluation { get; set; }

    [ForeignKey("RubricId")]
    [InverseProperty("EvaluationDetails")]
    public virtual Rubric? Rubric { get; set; }
}
