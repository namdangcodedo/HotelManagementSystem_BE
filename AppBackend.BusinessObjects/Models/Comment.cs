using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

[Table("Comment")]
public partial class Comment
{
    [Key]
    public int CommentId { get; set; }

    [ForeignKey("RoomType")]
    public int? RoomTypeId { get; set; }

    [ForeignKey("Comment")]
    public int? ReplyId { get; set; }

    [ForeignKey("Account")]
    public int? AccountId { get; set; }

    public string? Content { get; set; }
    public int? Rating { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }
    public virtual Account? Account { get; set; }

    public virtual ICollection<Comment> InverseReply { get; set; } = new List<Comment>();

    public virtual Comment? Reply { get; set; }

    public virtual RoomType? RoomType { get; set; }
}
