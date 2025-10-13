using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Rubric
{
    [Key]
    [Column("rubric_id")]
    public int RubricId { get; set; }

    [Column("criteria_name")]
    [StringLength(255)]
    public string? CriteriaName { get; set; }

    [Column("max_score", TypeName = "decimal(10, 2)")]
    public decimal? MaxScore { get; set; }

    [InverseProperty("Rubric")]
    public virtual ICollection<EvaluationDetail> EvaluationDetails { get; set; } = new List<EvaluationDetail>();

    [InverseProperty("Rubric")]
    public virtual ICollection<RubricWeight> RubricWeights { get; set; } = new List<RubricWeight>();
}
