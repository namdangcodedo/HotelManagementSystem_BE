using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CommentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.CommentService
{
    public interface ICommentService
    {
        Task<ResultModel> GetCommentsByRoomTypeId(GetCommentRequest request);
        Task<ResultModel> AddComment(AddCommentRequest request, int accountId);
        Task<ResultModel> UpdateComment(UpdateCommentRequest request, int accountId);
        Task<ResultModel> HideComment(int commentId);
    }
}
