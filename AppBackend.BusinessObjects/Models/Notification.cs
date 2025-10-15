using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Notification")]
public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [Required]
    [ForeignKey("Account")]
    public int AccountId { get; set; }

    [Required]
    [StringLength(255)]
    public string Message { get; set; } = null!;

    [Required]
    [ForeignKey("NotificationType")]
    public int NotificationTypeId { get; set; }
    public virtual CommonCode NotificationType { get; set; } = null!;

    [Required]
    public bool IsRead { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public virtual Account Account { get; set; } = null!;
}
