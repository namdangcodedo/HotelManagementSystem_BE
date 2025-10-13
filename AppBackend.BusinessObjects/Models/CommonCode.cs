using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("CommonCode")]
public class CommonCode
{
  [Key]
  public int CodeId { get; set; }

  [Required]
  [StringLength(50)]
  public string CodeType { get; set; } = null!;

  [Required]
  [StringLength(50)]
  public string CodeValue { get; set; } = null!;

  [Required]
  [StringLength(100)]
  public string CodeName { get; set; } = null!;

  [StringLength(255)]
  public string? Description { get; set; }

  public int? DisplayOrder { get; set; }

  [Required]
  public bool IsActive { get; set; }

  [Required]
  public DateTime CreatedAt { get; set; }

  public int? CreatedBy { get; set; }

  public DateTime? UpdatedAt { get; set; }

  public int? UpdatedBy { get; set; }

  [Required]
  [ForeignKey("GroupCommonCode")]
  public int GroupCommonCodeId { get; set; }
  public virtual GroupCommonCode GroupCommonCode { get; set; } = null!;
}
