using System;
using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.AttendanceModel
{
    public class PostCommentRequest
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
    }


}

