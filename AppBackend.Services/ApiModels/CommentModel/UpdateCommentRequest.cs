using System.ComponentModel.DataAnnotations;

namespace AppBackend.Services.ApiModels.CommentModel
{
    public class UpdateCommentRequest
    {
        [Required(ErrorMessage = "CommentId là bắt buộc")]
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Nội dung bình luận là bắt buộc")]
        [StringLength(1000, ErrorMessage = "Nội dung không được vượt quá 1000 ký tự")]
        public string Content { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int? Rating { get; set; }
    }
}

