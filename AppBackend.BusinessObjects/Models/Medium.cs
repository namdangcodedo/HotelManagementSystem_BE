using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Medium")]
public partial class Medium
{
    [Key]
    public int MediaId { get; set; }

    [StringLength(100)]
    public string? PublishId { get; set; }

    [Required]
    [StringLength(255)]
    public string FilePath { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    [Required]
    [StringLength(50)]
    public string ReferenceTable { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string ReferenceKey { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
