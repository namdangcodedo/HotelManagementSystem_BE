using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Project_Milestones")]
[Index("ProjectId", Name = "IX_Project_Milestones_Project")]
public partial class ProjectMilestone
{
    [Key]
    [Column("milestone_id")]
    public int MilestoneId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("due_date")]
    public DateOnly? DueDate { get; set; }

    [Column("status")]
    [StringLength(255)]
    public string? Status { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectMilestones")]
    public virtual Project Project { get; set; } = null!;
}
