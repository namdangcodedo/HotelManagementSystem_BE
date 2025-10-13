using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

public partial class Notification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("type")]
    [StringLength(255)]
    public string? Type { get; set; }

    [Column("is_read")]
    public bool? IsRead { get; set; }

    [Column("created_at")]
    [Precision(0)]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User? User { get; set; }
}
