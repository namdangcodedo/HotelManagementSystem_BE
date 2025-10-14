using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.ApiCore.Extension
{
    public static class SeedingData
    {
        public static async Task SeedAsync(HotelManagementContext context)
        {
            // Seed CommonCode if empty
            if (!context.CommonCodes.Any())
            {
                context.CommonCodes.AddRange(new List<CommonCode>
                {
                    // RoomType
                    new CommonCode { CodeType = "RoomType", CodeValue = "STD", CodeName = "Phòng tiêu chuẩn", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "RoomType", CodeValue = "DLX", CodeName = "Phòng cao cấp", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // Status
                    new CommonCode { CodeType = "Status", CodeValue = "ACTIVE", CodeName = "Hoạt động", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "Status", CodeValue = "INACTIVE", CodeName = "Không hoạt động", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "Status", CodeValue = "DELETED", CodeName = "Đã xóa", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "Status", CodeValue = "COMPLETED", CodeName = "Hoàn thành", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "Status", CodeValue = "PENDING", CodeName = "Chờ xử lý", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // EmployeeType
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "ADMIN", CodeName = "Quản trị viên", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "MANAGER", CodeName = "Quản lý", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "STAFF", CodeName = "Nhân viên", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // TaskType
                    new CommonCode { CodeType = "TaskType", CodeValue = "CLEANING", CodeName = "Dọn phòng", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "TaskType", CodeValue = "MAINTENANCE", CodeName = "Bảo trì", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // FeedbackType
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "COMPLAINT", CodeName = "Khiếu nại", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "SUGGESTION", CodeName = "Đề xuất", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "PRAISE", CodeName = "Khen ngợi", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // NotificationType
                    new CommonCode { CodeType = "NotificationType", CodeValue = "SYSTEM", CodeName = "Hệ thống", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "BOOKING", CodeName = "Đặt phòng", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "PROMOTION", CodeName = "Khuyến mãi", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // BookingType
                    new CommonCode { CodeType = "BookingType", CodeValue = "ONLINE", CodeName = "Đặt trực tuyến", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "BookingType", CodeValue = "WALKIN", CodeName = "Đặt tại quầy", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // PaymentStatus
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "PAID", CodeName = "Đã thanh toán", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "UNPAID", CodeName = "Chưa thanh toán", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "REFUNDED", CodeName = "Đã hoàn tiền", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // DepositStatus
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "PAID", CodeName = "Đã đặt cọc", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "UNPAID", CodeName = "Chưa đặt cọc", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    // PaymentMethod
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "CASH", CodeName = "Tiền mặt", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "CARD", CodeName = "Thẻ ngân hàng", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "BANK", CodeName = "Chuyển khoản", IsActive = true, CreatedAt = DateTime.UtcNow, GroupCommonCodeId = 1 }
                });
                await context.SaveChangesAsync();
            }

            // Seed Roles if empty
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(new List<Role>
                {
                    new Role { RoleValue = "Admin", RoleName = "Quản trị viên", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Manager", RoleName = "Quản lý", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Receptionist", RoleName = "Lễ tân", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Housekeeper", RoleName = "Nhân viên dọn phòng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Technician", RoleName = "Kỹ thuật viên", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "User", RoleName = "Khách hàng", IsActive = true, CreatedAt = DateTime.UtcNow }
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
