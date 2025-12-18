using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.ScheduleModel;

namespace AppBackend.Services.Services.ScheduleServices
{
    public interface IScheduleService
    {
        /// <summary>
        /// Lấy lịch làm việc theo tuần
        /// </summary>
        Task<ResultModel<WeeklyScheduleResponse>> GetWeeklyScheduleAsync(GetWeeklyScheduleRequest request);

        /// <summary>
        /// Thêm lịch làm việc mới
        /// </summary>
        Task<ResultModel> AddScheduleAsync(AddScheduleRequest request, int createdBy);

        /// <summary>
        /// Cập nhật lịch làm việc
        /// </summary>
        Task<ResultModel> UpdateScheduleAsync(UpdateScheduleRequest request, int updatedBy);

        /// <summary>
        /// Xoá lịch làm việc
        /// </summary>
        Task<ResultModel> DeleteScheduleAsync(int scheduleId);

        /// <summary>
        /// Lấy danh sách nhân viên có thể thêm vào ca làm việc
        /// </summary>
        Task<ResultModel<AvailableEmployeesResponse>> GetAvailableEmployeesAsync(CheckAvailableEmployeesRequest request);
    }
}
