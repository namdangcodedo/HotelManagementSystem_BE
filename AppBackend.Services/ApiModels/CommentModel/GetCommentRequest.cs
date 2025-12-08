using AppBackend.BusinessObjects.Dtos;

namespace AppBackend.Services.ApiModels.CommentModel
{
    public class GetCommentRequest : PagedRequestDto
    {
        public int? RoomTypeId { get; set; }
        public bool? isNewest { get; set; }

    }
}

