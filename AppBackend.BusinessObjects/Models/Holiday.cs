using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Holiday")]
public class Holiday
{
  [Key]
  public int HolidayId { get; set; }

  [Required]
  [StringLength(100)]
  public string Name { get; set; } = null!;

  [Required]
  public DateTime StartDate { get; set; }

  [Required]
  public DateTime EndDate { get; set; }

  [StringLength(255)]
  public string? Description { get; set; }

  [Required]
  public bool IsActive { get; set; }

  public DateTime? ExpiredDate { get; set; }

  public virtual ICollection<HolidayPricing> HolidayPricings { get; set; } = new List<HolidayPricing>();
}
