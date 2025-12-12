using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.SalaryModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.SalaryInfoServices
{
    public class SalaryInfoService : ISalaryInfoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly PaginationHelper _paginationHelper;

        public SalaryInfoService(IUnitOfWork unitOfWork, IMapper mapper, PaginationHelper paginationHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _paginationHelper = paginationHelper;
        }

        public async Task<ResultModel> GetAsync(GetSalaryInfoRequest request)
        {
            try
            {
                List<SalaryInfo> items;
                if (request.EmployeeId.HasValue)
                {
                    items = await _unitOfWork.SalaryInfos.GetByEmployeeIdAsync(request.EmployeeId.Value);
                }
                else
                {
                    items = await _unitOfWork.SalaryInfos.GetAllAsync();
                }

                if (request.Year.HasValue)
                {
                    items = items.Where(x => x.Year == request.Year.Value).ToList();
                }

                var dtos = _mapper.Map<List<SalaryInfoDto>>(items);
                var pageDtos = _paginationHelper.HandlePagination(dtos.Cast<dynamic>().ToList(), request.PageIndex, request.PageSize);
                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "SalaryInfo list",
                    Data = pageDtos,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = ex.Message,
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> GetByIdAsync(int id)
        {
            try
            {
                var item = await _unitOfWork.SalaryInfos.GetByIdAsync(id);
                if (item == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "SalaryInfo"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                var dto = _mapper.Map<SalaryInfoDto>(item);
                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Get SalaryInfo successful",
                    Data = dto,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = ex.Message,
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> CreateAsync(PostSalaryInfoRequest request)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == request.EmployeeId);
                if (employee == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                var entity = _mapper.Map<SalaryInfo>(request);
                entity.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.SalaryInfos.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();

                var dto = _mapper.Map<SalaryInfoDto>(entity);
                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Create SalaryInfo successful",
                    Data = dto,
                    StatusCode = StatusCodes.Status201Created
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = ex.Message,
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> UpdateAsync(int id, PostSalaryInfoRequest request)
        {
            try
            {
                var existing = await _unitOfWork.SalaryInfos.GetByIdAsync(id);
                if (existing == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "SalaryInfo"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                // update allowed fields
                existing.Year = request.Year;
                existing.BaseSalary = request.BaseSalary;
                existing.YearBonus = request.YearBonus;
                existing.Allowance = request.Allowance;
                existing.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SalaryInfos.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                var dto = _mapper.Map<SalaryInfoDto>(existing);
                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Update SalaryInfo successful",
                    Data = dto,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = ex.Message,
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        public async Task<ResultModel> DeleteAsync(int id)
        {
            try
            {
                var existing = await _unitOfWork.SalaryInfos.GetByIdAsync(id);
                if (existing == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "SalaryInfo"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }

                await _unitOfWork.SalaryInfos.DeleteAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Delete SalaryInfo successful",
                    Data = null,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = ex.Message,
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}