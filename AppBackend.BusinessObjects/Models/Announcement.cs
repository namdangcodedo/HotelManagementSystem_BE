using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Announcement
{
    [Key]
    [Column("announcement_id")]
    public int AnnouncementId { get; set; }

    [Column("admin_id")]
    public int? AdminId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("target_audience")]
    [StringLength(255)]
    public string? TargetAudience { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("Announcements")]
    public virtual User? Admin { get; set; }
}
