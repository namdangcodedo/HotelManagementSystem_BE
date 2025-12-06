using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Enums;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.EmployeeModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;

namespace AppBackend.Services.Services.AttendanceServices
{
    public class AttendanceService : IAttendaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly EncryptHelper _encryptHelper;

        public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, EncryptHelper encryptHelper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _encryptHelper = encryptHelper;
        }

        public async Task<ResultModel> GetEmployeeAttendance(GetAttendanceRequest request)
        {
            var employeeId = request.EmployeeId;
            if(employeeId == null)
            {
                var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
                if(employee != null)
                {
                    var attendances = await _unitOfWork.AttendenceRepository.GetAttendancesByEmployeeId((int)employeeId, request.Month, request.Year);
                    var attendanceDtos = _mapper.Map<List<AttendanceDTO>>(attendances);
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "Danh sách attendance",
                        Data = attendanceDtos,
                        StatusCode = StatusCodes.Status200OK
                    };
                }
            }
            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> GetEmployeeAttendInfo(GetAttendanceRequest request)
        {
            var employeeId = request.EmployeeId;
            if (employeeId == null)
            {
                var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
                if (employee != null)
                {
                    var attendInfos = await _unitOfWork.AttendenceRepository.GetAttendInfosByEmployeeId((int)employeeId, request.Year);
                    var attendInfoDTOs = _mapper.Map<List<EmpAttendInfoDTO>>(attendInfos);
                    return new ResultModel
                    {
                        IsSuccess = true,
                        ResponseCode = CommonMessageConstants.SUCCESS,
                        Message = "Attend Infos",
                        Data = attendInfoDTOs,
                        StatusCode = StatusCodes.Status200OK
                    };
                }
            }
            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> HandelEncryptData(EncryptTxtAttendanceRequest request)
        {
            string txtdata = _encryptHelper.DecryptString(request.EncryptTxt, request.Iv);

            var lines = txtdata.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i=i+2)
            {
                var lineCheckIn = lines[i];
                var lineCheckOut = lines[i+1];
                var checkInSplit = lineCheckIn.Split('|', StringSplitOptions.None);
                var checkOutSplit = lineCheckOut.Split('|', StringSplitOptions.None);

                var attendanceRecord = new Attendance
                {
                    EmployeeId = int.Parse(checkInSplit[0].Trim()),
                    CheckIn = DateTime.Parse(checkInSplit[2]),
                    CheckOut = DateTime.Parse(checkOutSplit[2].Trim()),
                    Status = AttendanceStatus.Attended.ToString(),
                    IsApproved = ApprovalStatus.Approved.ToString(),
                    CreatedAt = DateTime.Now
                };
                await _unitOfWork.AttendenceRepository.AddAsync(attendanceRecord);
                _unitOfWork.SaveChangesAsync();
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Insert attendances thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> InsertAttendances(PostAttendancesRequest request)
        {
            foreach (var attendance in request.Attendances)
            {
                var result = await UpsertAttendance(attendance);
                if(result.IsSuccess == false)
                {
                    return result;
                }
            }

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Insert attendances thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpsertAttendance(PostAttendanceRequest request)
        {
            var employeeId = request.EmployeeId;
            var attendanceId = request.AttendanceId;
            var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
            if (employee != null)
            {
                if (attendanceId != null)
                {
                    var existingAttendance = await _unitOfWork.AttendenceRepository.GetSingleAsync(a => a.AttendanceId == attendanceId);
                    if (existingAttendance != null)
                    {
                        _mapper.Map(request, existingAttendance);
                        _unitOfWork.AttendenceRepository.UpdateAsync(existingAttendance);
                        _unitOfWork.SaveChangesAsync();
                    }
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.NOT_FOUND,
                        Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Attendance"),
                        Data = null,
                        StatusCode = StatusCodes.Status404NotFound
                    };
                }
                else
                {
                    //init new attendance

                    //handle attendance data
                    var attendance = _mapper.Map<Attendance>(request);
                    var resultModel = await handleInitAttendance(attendance);
                    _unitOfWork.AttendenceRepository.AddAsync(attendance);
                    _unitOfWork.SaveChangesAsync();

                    return resultModel;
                }
                
            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }

        public async Task<ResultModel> handleInitAttendance(Attendance entity)
        {
            entity.CreatedAt = DateTime.Now;
            if (entity.Status.Equals(AttendanceStatus.Attended))
            {
                entity.IsApproved = ApprovalStatus.Approved.ToString();
                if(entity.CheckIn == null || entity.CheckOut == null)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "CheckIn và CheckOut không được để trống khi trạng thái là Attended",
                        Data = null,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                if(entity.CheckOut <= entity.CheckIn)
                {
                    return new ResultModel
                    {
                        IsSuccess = false,
                        ResponseCode = CommonMessageConstants.INVALID,
                        Message = "CheckOut phải lớn hơn CheckIn",
                        Data = null,
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }
            }
            else if (entity.Status.Equals(AttendanceStatus.AbsentWithLeave) ||
                entity.Status.Equals(AttendanceStatus.AbsentWithoutLeave))
            {
                entity.IsApproved = ApprovalStatus.Pending.ToString();
                //xử lý attendInfo
            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Upsert attendance thành công",
                Data = null,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> UpsertAttendInfo(PostAttendInfoRequest request)
        {
            var employeeId = request.EmployeeId;
            var attendInfoId = request.AttendInfoId;
            var employee = await _unitOfWork.Employees.GetSingleAsync(e => e.EmployeeId == employeeId);
            if(employee != null)
            {
                var attendInfo = _mapper.Map<EmpAttendInfo>(request);
                _unitOfWork.AttendenceRepository.UpsertAttendInfo(attendInfo);
                _unitOfWork.SaveChangesAsync();

                return new ResultModel
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Upsert AttendInfo thành công",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            return new ResultModel
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.NOT_FOUND,
                Message = string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "Nhân viên"),
                Data = null,
                StatusCode = StatusCodes.Status404NotFound
            };
        }
    }
}
