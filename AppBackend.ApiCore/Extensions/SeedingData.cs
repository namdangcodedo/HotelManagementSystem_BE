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
                    new CommonCode { CodeType = "RoomType", CodeValue = "Phòng tiêu chuẩn", CodeName = "Standard" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomType", CodeValue = "Phòng cao cấp", CodeName = "Deluxe" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // Status
                    new CommonCode { CodeType = "Status", CodeValue = "Hoạt động", CodeName = "Active" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Không hoạt động", CodeName = "Inactive" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã xóa", CodeName = "Deleted" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Hoàn thành", CodeName = "Completed" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Chờ xử lý", CodeName = "Pending" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // EmployeeType
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản trị viên", CodeName = "Admin" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản lý", CodeName = "Manager" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Nhân viên", CodeName = "Staff" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // TaskType
                    new CommonCode { CodeType = "TaskType", CodeValue = "Dọn phòng", CodeName = "Cleaning" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Bảo trì", CodeName = "Maintenance" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // FeedbackType
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khiếu nại", CodeName = "Complaint" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Đề xuất", CodeName = "Suggestion" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khen ngợi", CodeName = "Praise" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // NotificationType
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Hệ thống", CodeName = "System" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Đặt phòng", CodeName = "BookingDtos" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Khuyến mãi", CodeName = "Promotion" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // BookingType
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt trực tuyến", CodeName = "Online" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt tại quầy", CodeName = "Walkin" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // PaymentStatus
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã thanh toán", CodeName = "Paid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Chưa thanh toán", CodeName = "Unpaid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã hoàn tiền", CodeName = "Refunded" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // DepositStatus
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Đã đặt cọc", CodeName = "Paid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Chưa đặt cọc", CodeName = "Unpaid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    // PaymentMethod
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Tiền mặt", CodeName = "Cash" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Thẻ ngân hàng", CodeName = "Card" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Chuyển khoản", CodeName = "Bank" , IsActive = true, CreatedAt = DateTime.UtcNow }
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

            // Seed AmenityServices if empty
            if (!context.Amenities.Any())
            {
                context.Amenities.AddRange(new List<Amenity>
                {
                    new Amenity { AmenityName = "Wifi miễn phí", Description = "Kết nối internet tốc độ cao miễn phí cho khách.", Price = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Bữa sáng miễn phí", Description = "Bữa sáng buffet đa dạng cho khách lưu trú.", Price = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Dịch vụ giặt ủi", Description = "Giặt ủi quần áo theo yêu cầu.", Price = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Hồ bơi", Description = "Hồ bơi ngoài trời dành cho khách.", Price = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Xe đưa đón sân bay", Description = "Dịch vụ xe đưa đón sân bay tiện lợi.", Price = 200000, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Phòng gym", Description = "Phòng tập thể hình hiện đại.", Price = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Amenity { AmenityName = "Spa & Massage", Description = "Dịch vụ spa và massage thư giãn.", Price = 300000, IsActive = true, CreatedAt = DateTime.UtcNow }
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
