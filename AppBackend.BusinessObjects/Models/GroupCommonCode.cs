using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("GroupCommonCode")]
public class GroupCommonCode
{
    [Key]
    public int GroupCommonCodeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    public virtual ICollection<CommonCode> CommonCodes { get; set; } = new List<CommonCode>();
}

