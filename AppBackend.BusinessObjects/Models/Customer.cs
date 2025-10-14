using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Customer")]
public partial class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [ForeignKey("Account")]
    public int AccountId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(20)]
    public string? IdentityCard { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [ForeignKey("AvatarMedium")]
    public int? AvatarMediaId { get; set; }

    public virtual Medium? AvatarMedium { get; set; }
}
