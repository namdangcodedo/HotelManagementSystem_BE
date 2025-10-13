using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Project_Assets")]
[Index("ProjectId", Name = "idx_assets_project")]
[Index("AssetType", "Visibility", Name = "idx_assets_type")]
public partial class ProjectAsset
{
    [Key]
    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("asset_type")]
    [StringLength(255)]
    public string? AssetType { get; set; }

    [Column("file_url")]
    public string? FileUrl { get; set; }

    [Column("uploaded_at")]
    [Precision(0)]
    public DateTime? UploadedAt { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }

    [Column("mime_type")]
    [StringLength(255)]
    public string? MimeType { get; set; }

    [Column("file_ext")]
    [StringLength(255)]
    public string? FileExt { get; set; }

    [Column("size_bytes")]
    public long? SizeBytes { get; set; }

    [Column("is_primary")]
    public bool? IsPrimary { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("uploaded_by")]
    public int? UploadedBy { get; set; }

    [Column("updated_at")]
    [Precision(0)]
    public DateTime? UpdatedAt { get; set; }

    [Column("visibility")]
    [StringLength(255)]
    public string? Visibility { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectAssets")]
    public virtual Project? Project { get; set; }
}
