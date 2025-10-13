using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Project_Submissions")]
public partial class ProjectSubmission
{
    [Key]
    [Column("submission_id")]
    public int SubmissionId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("submitted_by")]
    public int SubmittedBy { get; set; }

    [Column("status")]
    [StringLength(255)]
    public string Status { get; set; } = null!;

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectSubmissions")]
    public virtual Project Project { get; set; } = null!;

    [InverseProperty("Submission")]
    public virtual ICollection<ProjectApprovalHistory> ProjectApprovalHistories { get; set; } = new List<ProjectApprovalHistory>();

    [ForeignKey("SubmittedBy")]
    [InverseProperty("ProjectSubmissions")]
    public virtual User SubmittedByNavigation { get; set; } = null!;
}
