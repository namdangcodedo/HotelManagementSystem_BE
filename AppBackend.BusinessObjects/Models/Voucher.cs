using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

public enum DiscountType
{
    Percentage = 0,
    Amount = 1
}

[Table("Voucher")]
public class Voucher
{
    [Key]
    public int VoucherId { get; set; }
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = null!;
    [StringLength(255)]
    public string? Description { get; set; }
    [Required]
    public DiscountType DiscountType { get; set; }
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    [Required]
    public bool IsActive { get; set; }
    public int UsageLimit { get; set; } = 1;
    public int UsedCount { get; set; } = 0;
    public DateTime? ExpiredDate { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
