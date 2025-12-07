using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBackend.BusinessObjects.Models;

public partial class CommentDTO
{
    public int CommentId { get; set; }
    public int? RoomId { get; set; }
    public int? ReplyId { get; set; }
    public int? AccountId { get; set; }
    public string? Content { get; set; }
    public int? Rating { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? CreatedTime { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }
    public virtual ICollection<CommentDTO> InverseReply { get; set; } = new List<CommentDTO>();
    public virtual CommentDTO? Reply { get; set; }
}
