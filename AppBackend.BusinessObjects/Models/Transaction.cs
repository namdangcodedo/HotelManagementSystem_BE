using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Transaction")]
public class Transaction
{
    [Key]
    public int TransactionId { get; set; }

    [Required]
    [ForeignKey("Booking")]
    public int BookingId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [ForeignKey("PaymentMethod")]
    public int PaymentMethodId { get; set; }
    public virtual CommonCode PaymentMethod { get; set; } = null!;

    [Required]
    [ForeignKey("PaymentStatus")]
    public int PaymentStatusId { get; set; }
    public virtual CommonCode PaymentStatus { get; set; } = null!;

    [StringLength(100)]
    public string? TransactionRef { get; set; }

    [StringLength(100)]
    public string? OrderCode { get; set; } // Mã giao dịch với hệ thống ngân hàng ngoài

    [Required]
    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DepositAmount { get; set; }

    [ForeignKey("DepositStatus")]
    public int? DepositStatusId { get; set; }
    public virtual CommonCode? DepositStatus { get; set; }

    public DateTime? DepositDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Required]
    [ForeignKey("CommonCode")]
    public int TransactionStatusId { get; set; }
    public virtual CommonCode TransactionStatus { get; set; } = null!;
}
