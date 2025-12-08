using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.CommentModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.Services.Services.CommentService
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper, EncryptHelper encryptHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ResultModel> AddComment(PostCommentRequest request, int accountId)
        {
            if(accountId != null)
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

                if(account == null) 
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Không tìm thấy tài khoản"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };

                var commentEntity = _mapper.Map<Comment>(request);
                commentEntity.AccountId = accountId;
                await _unitOfWork.Comments.AddAsync(commentEntity);
                _unitOfWork.SaveChangesAsync();
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Thêm comment thành công",
                    Data = null,
                    StatusCode = StatusCodes.Status200OK
                };
            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.INVALID,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Account id là bắt buộc"),
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        public async Task<ResultModel> GetCommentsByRoomTypeId(GetCommentRequest request)
        {
            if(request.RoomTypeId != null)
            {
                var comments = await _unitOfWork.Comments.GetCommentsByRoomTypeId(request.RoomTypeId.Value); 
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Comment",
                    Data = comments,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.BAD_REQUEST,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Id phòng là bắt buộc"),
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
            
        }

        public async Task<ResultModel> UpdateComment(PostCommentRequest request, int accountId)
        {
            if (accountId != null)
            {
                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

                if (account == null)
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Không tìm thấy tài khoản"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };

                var commentId = request.CommentId;
                if (commentId != null)
                {
                    var existingComment = _unitOfWork.Comments.GetByIdAsync(commentId);
                    if (existingComment != null)
                    {
                        var commentEntity = _mapper.Map<Comment>(request);
                        commentEntity.UpdatedAt = DateTime.Now;
                        await _unitOfWork.Comments.UpdateAsync(commentEntity);
                        _unitOfWork.SaveChangesAsync();
                    }
                }
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.BAD_REQUEST,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Id comment là bắt buộc"),
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };

            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.INVALID,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Account Id là bắt buộc"),
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
