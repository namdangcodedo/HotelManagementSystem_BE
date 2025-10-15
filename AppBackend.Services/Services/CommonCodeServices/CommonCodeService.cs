using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.CommonCodeModel;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.CommonCodeServices
{
    public class CommonCodeService : ICommonCodeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CommonCodeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ResultModel> GetCommonCodeListAsync(GetCommonCodeListRequest request)
        {
            var query = _unitOfWork.CommonCodes.FindAsync(c => true);
            var commonCodes = (await query).AsQueryable();

            // Lọc theo CodeType
            if (!string.IsNullOrWhiteSpace(request.CodeType))
            {
                commonCodes = commonCodes.Where(c => c.CodeType == request.CodeType);
            }

            // Lọc theo trạng thái
            if (request.IsActive.HasValue)
            {
                commonCodes = commonCodes.Where(c => c.IsActive == request.IsActive.Value);
            }

            // Tìm kiếm theo CodeValue hoặc CodeName
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                commonCodes = commonCodes.Where(c =>
                    c.CodeValue.Contains(request.Search) ||
                    c.CodeName.Contains(request.Search) ||
                    c.CodeType.Contains(request.Search));
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                commonCodes = request.SortDesc
                    ? commonCodes.OrderByDescending(c => Microsoft.EntityFrameworkCore.EF.Property<object>(c, request.SortBy))
                    : commonCodes.OrderBy(c => Microsoft.EntityFrameworkCore.EF.Property<object>(c, request.SortBy));
            }
            else
            {
                commonCodes = commonCodes.OrderBy(c => c.CodeType).ThenBy(c => c.DisplayOrder);
            }

            // Tổng số bản ghi
            var totalRecords = commonCodes.Count();

            // Phân trang
            var pagedCommonCodes = commonCodes
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var commonCodeDtos = _mapper.Map<List<CommonCodeDto>>(pagedCommonCodes);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = new
                {
                    Items = commonCodeDtos,
                    TotalRecords = totalRecords,
                    request.PageIndex,
                    request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize)
                },
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCommonCodeByIdAsync(int codeId)
        {
            var commonCode = await _unitOfWork.CommonCodes.GetByIdAsync(codeId);

            if (commonCode == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Common Code"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var commonCodeDto = _mapper.Map<CommonCodeDto>(commonCode);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = commonCodeDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCodeTypeListAsync()
        {
            var commonCodes = await _unitOfWork.CommonCodes.GetAllAsync();
            
            var codeTypeList = commonCodes
                .GroupBy(c => c.CodeType)
                .Select(g => new GetCodeTypeListResponse
                {
                    CodeType = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.CodeType)
                .ToList();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = codeTypeList,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCommonCodesByTypeAsync(string codeType)
        {
            var commonCodes = await _unitOfWork.CommonCodes.GetByTypeAsync(codeType);
            var commonCodeDtos = _mapper.Map<List<CommonCodeDto>>(commonCodes);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = commonCodeDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> AddCommonCodeAsync(AddCommonCodeRequest request)
        {
            // Kiểm tra trùng lặp
            var existing = (await _unitOfWork.CommonCodes.FindAsync(c => 
                c.CodeType == request.CodeType && c.CodeValue == request.CodeValue)).FirstOrDefault();
            
            if (existing != null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.EXISTED,
                    Message = string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Common Code"),
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var commonCode = new CommonCode
            {
                CodeType = request.CodeType,
                CodeValue = request.CodeValue,
                CodeName = request.CodeName,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                UpdatedAt = null,
                UpdatedBy = null
            };

            await _unitOfWork.CommonCodes.AddAsync(commonCode);
            await _unitOfWork.SaveChangesAsync();

            var commonCodeDto = _mapper.Map<CommonCodeDto>(commonCode);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Thêm Common Code thành công",
                Data = commonCodeDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpdateCommonCodeAsync(UpdateCommonCodeRequest request)
        {
            var commonCode = await _unitOfWork.CommonCodes.GetByIdAsync(request.CodeId);

            if (commonCode == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Common Code"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Cập nhật các trường
            if (!string.IsNullOrWhiteSpace(request.CodeType))
            {
                commonCode.CodeType = request.CodeType;
            }

            if (!string.IsNullOrWhiteSpace(request.CodeValue))
            {
                commonCode.CodeValue = request.CodeValue;
            }

            if (!string.IsNullOrWhiteSpace(request.CodeName))
            {
                commonCode.CodeName = request.CodeName;
            }

            if (request.Description != null)
            {
                commonCode.Description = request.Description;
            }

            if (request.DisplayOrder.HasValue)
            {
                commonCode.DisplayOrder = request.DisplayOrder.Value;
            }

            if (request.IsActive.HasValue)
            {
                commonCode.IsActive = request.IsActive.Value;
            }

            commonCode.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CommonCodes.UpdateAsync(commonCode);
            await _unitOfWork.SaveChangesAsync();

            var commonCodeDto = _mapper.Map<CommonCodeDto>(commonCode);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Cập nhật Common Code thành công",
                Data = commonCodeDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteCommonCodeAsync(int codeId)
        {
            var commonCode = await _unitOfWork.CommonCodes.GetByIdAsync(codeId);

            if (commonCode == null)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Common Code"),
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            await _unitOfWork.CommonCodes.DeleteAsync(commonCode);
            await _unitOfWork.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Xóa Common Code thành công",
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}

