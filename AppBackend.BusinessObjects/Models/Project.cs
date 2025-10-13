using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Project
{
    [Key]
    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("class_id")]
    public int? ClassId { get; set; }

    [Column("leader_id")]
    public int? LeaderId { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [Column("status")]
    [StringLength(255)]
    public string? Status { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("Projects")]
    public virtual Class? Class { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

    [InverseProperty("Project")]
    public virtual ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();

    [ForeignKey("LeaderId")]
    [InverseProperty("Projects")]
    public virtual User? Leader { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<LiveDemo> LiveDemos { get; set; } = new List<LiveDemo>();

    [InverseProperty("Project")]
    public virtual ICollection<ProjectAsset> ProjectAssets { get; set; } = new List<ProjectAsset>();

    [InverseProperty("Project")]
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    [InverseProperty("Project")]
    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    [InverseProperty("Project")]
    public virtual ICollection<ProjectSubmission> ProjectSubmissions { get; set; } = new List<ProjectSubmission>();

    [InverseProperty("Project")]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
