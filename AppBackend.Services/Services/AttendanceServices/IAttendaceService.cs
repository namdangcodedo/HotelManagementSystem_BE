using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.AttendanceModel;
using AppBackend.Services.ApiModels.EmployeeModel;

namespace AppBackend.Services.Services.AttendanceServices
{
    public interface IAttendaceService
    {
        Task<ResultModel> HandelEncryptData(EncryptTxtAttendanceRequest request);
        Task<ResultModel> GetEmployeeAttendInfo(GetAttendanceRequest request);
        Task<ResultModel> GetEmployeeAttendance(GetAttendanceRequest request);
        Task<ResultModel> UpsertAttendance(PostAttendanceRequest request);
        Task<ResultModel> InsertAttendances(PostAttendancesRequest request);
        Task<ResultModel> UpsertAttendInfo(PostAttendInfoRequest request);
        Task<ResultModel> GetStaticInfo(GetAttendanceRequest request);


    }
}
