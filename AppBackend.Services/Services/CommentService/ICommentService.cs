using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;
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
        Task<ResultModel> GetCommentsByPostId(GetCommentRequest request);
        Task<ResultModel> AddComment(PostCommentRequest request, int accountId);
        Task<ResultModel> UpdateComment(PostCommentRequest request, int accountId);

    }
}
