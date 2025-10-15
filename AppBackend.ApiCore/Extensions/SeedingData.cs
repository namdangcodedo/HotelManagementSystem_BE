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
                    new CommonCode { CodeType = "RoomType", CodeValue = "Phòng VIP", CodeName = "VIP" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomType", CodeValue = "Suite", CodeName = "Suite" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // Status
                    new CommonCode { CodeType = "Status", CodeValue = "Hoạt động", CodeName = "Active" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Không hoạt động", CodeName = "Inactive" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã xóa", CodeName = "Deleted" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Hoàn thành", CodeName = "Completed" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Chờ xử lý", CodeName = "Pending" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đang xử lý", CodeName = "Processing" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã hủy", CodeName = "Cancelled" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // EmployeeType
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản trị viên", CodeName = "Admin" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 1 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản lý", CodeName = "Manager" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 2 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Lễ tân", CodeName = "Receptionist" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 3 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Nhân viên dọn phòng", CodeName = "Housekeeper" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 4 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Kỹ thuật viên", CodeName = "Technician" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 5 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Bảo vệ", CodeName = "Security" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 6 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Đầu bếp", CodeName = "Chef" , IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 7 },
                    
                    // TaskType
                    new CommonCode { CodeType = "TaskType", CodeValue = "Dọn phòng", CodeName = "Cleaning" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Bảo trì", CodeName = "Maintenance" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Kiểm tra", CodeName = "Inspection" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // FeedbackType
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khiếu nại", CodeName = "Complaint" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Đề xuất", CodeName = "Suggestion" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khen ngợi", CodeName = "Praise" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // NotificationType
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Hệ thống", CodeName = "System" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Đặt phòng", CodeName = "Booking" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Khuyến mãi", CodeName = "Promotion" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // BookingType
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt trực tuyến", CodeName = "Online" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt tại quầy", CodeName = "Walkin" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // PaymentStatus
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã thanh toán", CodeName = "Paid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Chưa thanh toán", CodeName = "Unpaid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã hoàn tiền", CodeName = "Refunded" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Thanh toán một phần", CodeName = "PartiallyPaid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // DepositStatus
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Đã đặt cọc", CodeName = "Paid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Chưa đặt cọc", CodeName = "Unpaid" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // PaymentMethod
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Tiền mặt", CodeName = "Cash" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Thẻ ngân hàng", CodeName = "Card" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Chuyển khoản", CodeName = "Bank" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Ví điện tử", CodeName = "EWallet" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // RoomStatus
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Trống", CodeName = "Available" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đã đặt", CodeName = "Booked" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đang sử dụng", CodeName = "Occupied" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đang dọn dẹp", CodeName = "Cleaning" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Bảo trì", CodeName = "Maintenance" , IsActive = true, CreatedAt = DateTime.UtcNow }
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

            // Seed Employee Accounts and Employees if empty
            if (!context.Employees.Any())
            {
                // Tạo tài khoản Admin
                var adminAccount = new Account
                {
                    Username = "admin",
                    Email = "admin@hotel.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12),
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Accounts.AddAsync(adminAccount);
                await context.SaveChangesAsync();

                // Lấy EmployeeType cho Admin
                var adminEmployeeType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "EmployeeType" && c.CodeName == "Admin");

                if (adminEmployeeType != null)
                {
                    var adminEmployee = new Employee
                    {
                        AccountId = adminAccount.AccountId,
                        FullName = "Administrator",
                        PhoneNumber = "0900000001",
                        EmployeeTypeId = adminEmployeeType.CodeId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Employees.AddAsync(adminEmployee);
                }

                // Tạo tài khoản Manager
                var managerAccount = new Account
                {
                    Username = "manager",
                    Email = "manager@hotel.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123", 12),
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Accounts.AddAsync(managerAccount);
                await context.SaveChangesAsync();

                var managerEmployeeType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "EmployeeType" && c.CodeName == "Manager");

                if (managerEmployeeType != null)
                {
                    var managerEmployee = new Employee
                    {
                        AccountId = managerAccount.AccountId,
                        FullName = "Nguyễn Văn Quản Lý",
                        PhoneNumber = "0900000002",
                        EmployeeTypeId = managerEmployeeType.CodeId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Employees.AddAsync(managerEmployee);
                }

                // Tạo tài khoản Receptionist
                var receptionistAccount = new Account
                {
                    Username = "receptionist",
                    Email = "receptionist@hotel.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123", 12),
                    IsLocked = false,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Accounts.AddAsync(receptionistAccount);
                await context.SaveChangesAsync();

                var receptionistEmployeeType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "EmployeeType" && c.CodeName == "Receptionist");

                if (receptionistEmployeeType != null)
                {
                    var receptionistEmployee = new Employee
                    {
                        AccountId = receptionistAccount.AccountId,
                        FullName = "Trần Thị Lễ Tân",
                        PhoneNumber = "0900000003",
                        EmployeeTypeId = receptionistEmployeeType.CodeId,
                        HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Employees.AddAsync(receptionistEmployee);
                }

                await context.SaveChangesAsync();

                // Gán vai trò cho các nhân viên
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleValue == "Admin");
                var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleValue == "Manager");
                var receptionistRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleValue == "Receptionist");

                if (adminRole != null)
                {
                    await context.AccountRoles.AddAsync(new AccountRole
                    {
                        AccountId = adminAccount.AccountId,
                        RoleId = adminRole.RoleId
                    });
                }

                if (managerRole != null)
                {
                    await context.AccountRoles.AddAsync(new AccountRole
                    {
                        AccountId = managerAccount.AccountId,
                        RoleId = managerRole.RoleId
                    });
                }

                if (receptionistRole != null)
                {
                    await context.AccountRoles.AddAsync(new AccountRole
                    {
                        AccountId = receptionistAccount.AccountId,
                        RoleId = receptionistRole.RoleId
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
