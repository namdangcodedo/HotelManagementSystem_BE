using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Dtos;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.UnitOfWork;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.SalaryModel;
using AppBackend.Services.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;

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

        public async Task<ResultModel> CalculateMonthlySalary(CalculateSalaryRequest request)
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

                // Use SalaryInfo for the requested year if present, otherwise fallback to employee base salary
                var targetYear = request.Year;
                var targetMonth = request.Month;

                var salaryInfos = await _unitOfWork.SalaryInfos.GetByEmployeeIdAsync(employee.EmployeeId);
                var salaryInfo = salaryInfos?.FirstOrDefault(s => s.Year == targetYear);
                var baseSalary = salaryInfo?.BaseSalary ?? employee.BaseSalary;

                decimal standardMonthlyHours = request.StandardMonthlyHours ?? 208m;
                decimal overtimeMultiplier = request.OvertimeMultiplier ?? 1.5m;

                // Fetch attendances for the employee for the requested month/year
                var monthAttendances = await _unitOfWork.AttendenceRepository.GetAttendancesByEmployeeId(employee.EmployeeId, month: targetMonth, year: targetYear);

                decimal totalNormalHours = 0m;
                decimal totalOvertimeHours = 0m;

                foreach (var a in monthAttendances)
                {
                    if (a.CheckIn == null) continue;
                    if (a.CheckOut == null) continue;

                    var inSpan = a.CheckIn.ToTimeSpan();
                    var outSpan = a.CheckOut.Value.ToTimeSpan();

                    TimeSpan duration;
                    if (outSpan < inSpan)
                    {
                        // overnight shift spanning to next day
                        duration = (outSpan + TimeSpan.FromHours(24)) - inSpan;
                    }
                    else
                    {
                        duration = outSpan - inSpan;
                    }

                    var workedHours = (decimal)duration.TotalHours;
                    if (workedHours <= 0) continue;

                    var normal = Math.Min(8m, workedHours);
                    var ot = Math.Max(0m, workedHours - 8m);

                    totalNormalHours += normal;
                    totalOvertimeHours += ot;
                }

                // Count sick days and absent days based on Attendance.Status
                var sickDays = monthAttendances
                    .Where(a => a.Status != null && a.Status.Equals(BusinessObjects.Enums.AttendanceStatus.AbsentWithLeave.ToString(), StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Workdate.Date)
                    .Distinct()
                    .Count();

                var absentDays = monthAttendances
                    .Where(a => a.Status != null && a.Status.Equals(BusinessObjects.Enums.AttendanceStatus.AbsentWithoutLeave.ToString(), StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Workdate.Date)
                    .Distinct()
                    .Count();

                decimal hourlyRate = standardMonthlyHours > 0 ? decimal.Round(baseSalary / standardMonthlyHours, 4) : 0m;

                // Prorated base pay based on normal hours (capped at baseSalary)
                decimal basePay = 0m;
                if (standardMonthlyHours > 0)
                {
                    basePay = decimal.Round(baseSalary * Math.Min(1m, totalNormalHours / standardMonthlyHours), 2);
                }

                decimal overtimePay = decimal.Round(totalOvertimeHours * hourlyRate * overtimeMultiplier, 2);
                decimal totalPay = decimal.Round(basePay + overtimePay, 2);

                // Use extracted helper to build excel bytes
                var (fileBytes, fileName, contentType) = BuildSalaryExcel(
                    employee,
                    targetYear,
                    targetMonth,
                    baseSalary,
                    Math.Round(totalNormalHours, 2),
                    Math.Round(totalOvertimeHours, 2),
                    sickDays,
                    absentDays,
                    basePay,
                    overtimePay,
                    totalPay);

                var fileInfo = new
                {
                    FileName = fileName,
                    ContentType = contentType,
                    FileBytes = fileBytes
                };

                return new ResultModel
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Salary Excel generated",
                    Data = fileInfo,
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

        // Extracted Excel generation logic into a single reusable method
        private (byte[] FileBytes, string FileName, string ContentType) BuildSalaryExcel(
            Employee employee,
            int year,
            int month,
            decimal baseSalary,
            decimal totalNormalHours,
            decimal totalOvertimeHours,
            int sickDays,
            int absentDays,
            decimal basePay,
            decimal overtimePay,
            decimal totalPay)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Salary");

            // Header
            ws.Cell(1, 1).Value = "Salary Statement";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Value = "Employee ID:";
            ws.Cell(2, 2).Value = employee.EmployeeId;
            ws.Cell(3, 1).Value = "Employee Name:";
            ws.Cell(3, 2).Value = employee.FullName ?? string.Empty;
            ws.Cell(4, 1).Value = "Year:";
            ws.Cell(4, 2).Value = year;
            ws.Cell(5, 1).Value = "Month:";
            ws.Cell(5, 2).Value = month;

            // Table headers
            ws.Cell(7, 1).Value = "Item";
            ws.Cell(7, 2).Value = "Value";
            ws.Row(7).Style.Font.Bold = true;

            // Rows
            int r = 8;
            ws.Cell(r++, 1).Value = "Base Salary";
            ws.Cell(r - 1, 2).Value = baseSalary;
            ws.Cell(r++, 1).Value = "Total Work Hours";
            ws.Cell(r - 1, 2).Value = totalNormalHours;
            ws.Cell(r++, 1).Value = "Total Overtime Hours";
            ws.Cell(r - 1, 2).Value = totalOvertimeHours;
            ws.Cell(r++, 1).Value = "Sick Days (with leave)";
            ws.Cell(r - 1, 2).Value = sickDays;
            ws.Cell(r++, 1).Value = "Absent Days (without leave)";
            ws.Cell(r - 1, 2).Value = absentDays;
            ws.Cell(r++, 1).Value = "Base Pay (prorated)";
            ws.Cell(r - 1, 2).Value = basePay;
            ws.Cell(r++, 1).Value = "Overtime Pay";
            ws.Cell(r - 1, 2).Value = overtimePay;
            ws.Cell(r++, 1).Value = "Total Salary";
            ws.Cell(r - 1, 2).Value = totalPay;

            // Format currency column
            var currencyRange = ws.Range(8, 2, r - 1, 2);
            currencyRange.Style.NumberFormat.Format = "#,##0.00";

            // Adjust column widths
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            var fileName = $"Salary_{employee.EmployeeId}_{year}_{month}.xlsx";
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return (bytes, fileName, contentType);
        }
    }
}