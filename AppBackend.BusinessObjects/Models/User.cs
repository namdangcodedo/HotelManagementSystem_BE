using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Index("Email", Name = "UQ__Users__AB6E6164D0B4B9B9", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string? FullName { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string? PasswordHash { get; set; }

    [Column("phone")]
    [StringLength(255)]
    public string? Phone { get; set; }

    [Column("role_id")]
    public int? RoleId { get; set; }

    [Column("avatar_url")]
    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Admin")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    [InverseProperty("Student")]
    public virtual ICollection<ClassEnrollment> ClassEnrollments { get; set; } = new List<ClassEnrollment>();

    [InverseProperty("Instructor")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [InverseProperty("Instructor")]
    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

    [InverseProperty("Receiver")]
    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("Reviewer")]
    public virtual ICollection<ProjectApprovalHistory> ProjectApprovalHistories { get; set; } = new List<ProjectApprovalHistory>();

    [InverseProperty("User")]
    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    [InverseProperty("SubmittedByNavigation")]
    public virtual ICollection<ProjectSubmission> ProjectSubmissions { get; set; } = new List<ProjectSubmission>();

    [InverseProperty("Leader")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role? Role { get; set; }
}
