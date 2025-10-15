using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Role")]
public class Role
{
  [Key]
  public int RoleId { get; set; }
  [Required]
  [StringLength(50)]
  public string RoleValue { get; set; } = string.Empty; // English value for authentication
  [Required]
  [StringLength(50)]
  public string RoleName { get; set; } = string.Empty; // Vietnamese display name
  [StringLength(255)]
  public string? Description { get; set; }
  [Required]
  public DateTime CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  [Required]
  public bool IsActive { get; set; }
  public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}
