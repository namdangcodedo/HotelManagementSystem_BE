using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.CommentModel
{
    public class GetCommentRequest : PagedRequestDto
    {
        public int? RoomTypeId { get; set; }
        public bool? IsNewest { get; set; }
        public int? ParentCommentId { get; set; } // Lấy các comment con của 1 comment cha
        public bool IncludeReplies { get; set; } = true; // Có lấy kèm reply hay không
        public int MaxReplyDepth { get; set; } = 1; // Độ sâu của reply (mặc định 1 cấp)
    }
}
