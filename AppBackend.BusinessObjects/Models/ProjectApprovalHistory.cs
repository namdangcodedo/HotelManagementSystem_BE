using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Project_Approval_History")]
public partial class ProjectApprovalHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("submission_id")]
    public int SubmissionId { get; set; }

    [Column("reviewer_id")]
    public int ReviewerId { get; set; }

    [Column("action")]
    [StringLength(255)]
    public string Action { get; set; } = null!;

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("acted_at")]
    [Precision(0)]
    public DateTime? ActedAt { get; set; }

    [ForeignKey("ReviewerId")]
    [InverseProperty("ProjectApprovalHistories")]
    public virtual User Reviewer { get; set; } = null!;

    [ForeignKey("SubmissionId")]
    [InverseProperty("ProjectApprovalHistories")]
    public virtual ProjectSubmission Submission { get; set; } = null!;
}
