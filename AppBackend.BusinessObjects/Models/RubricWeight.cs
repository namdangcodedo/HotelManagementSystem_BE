using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Rubric_Weights")]
[Index("ClassId", "RubricId", Name = "uq_class_rubric", IsUnique = true)]
public partial class RubricWeight
{
    [Key]
    [Column("weight_id")]
    public int WeightId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("rubric_id")]
    public int RubricId { get; set; }

    [Column("weight_ratio", TypeName = "decimal(10, 2)")]
    public decimal WeightRatio { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("RubricWeights")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("RubricId")]
    [InverseProperty("RubricWeights")]
    public virtual Rubric Rubric { get; set; } = null!;
}
