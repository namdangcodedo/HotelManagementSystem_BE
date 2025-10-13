using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.BusinessObjects.Models;

[Table("Project_Members")]
[Index("ProjectId", "UserId", Name = "uq_project_user", IsUnique = true)]
public partial class ProjectMember
{
    [Key]
    [Column("member_id")]
    public int MemberId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("role_in_project")]
    [StringLength(255)]
    public string? RoleInProject { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectMembers")]
    public virtual Project? Project { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ProjectMembers")]
    public virtual User? User { get; set; }
}
