using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.ApiCore.Extension
{
    public static class SeedingData
    {
        public static async Task SeedAsync(HotelManagementContext context)
        {
            // Seed Roles first (vì Employee sẽ cần reference đến Role)
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(new List<Role>
                {
                    new Role { RoleValue = "Admin", RoleName = "Quản trị viên", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Manager", RoleName = "Quản lý", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Receptionist", RoleName = "Lễ tân", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Housekeeper", RoleName = "Nhân viên dọn phòng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Technician", RoleName = "Kỹ thuật viên", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Security", RoleName = "Bảo vệ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Chef", RoleName = "Đầu bếp", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "Waiter", RoleName = "Nhân viên phục vụ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Role { RoleValue = "User", RoleName = "Khách hàng", IsActive = true, CreatedAt = DateTime.UtcNow }
                });
                await context.SaveChangesAsync();
            }

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
                    
                    // EmployeeType - ĐỒNG BỘ HOÀN TOÀN với Role
                    // CodeValue = Role.RoleName (Tiếng Việt) | CodeName = Role.RoleValue (English - dùng để mapping)
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản trị viên", CodeName = "Admin" , Description = "Quản trị viên hệ thống", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 1 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Quản lý", CodeName = "Manager" , Description = "Quản lý khách sạn", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 2 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Lễ tân", CodeName = "Receptionist" , Description = "Nhân viên lễ tân", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 3 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Nhân viên dọn phòng", CodeName = "Housekeeper" , Description = "Nhân viên dọn dẹp phòng", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 4 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Kỹ thuật viên", CodeName = "Technician" , Description = "Kỹ thuật viên bảo trì", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 5 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Bảo vệ", CodeName = "Security" , Description = "Nhân viên bảo vệ", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 6 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Đầu bếp", CodeName = "Chef" , Description = "Đầu bếp nhà hàng", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 7 },
                    new CommonCode { CodeType = "EmployeeType", CodeValue = "Nhân viên phục vụ", CodeName = "Waiter" , Description = "Nhân viên phục vụ nhà hàng", IsActive = true, CreatedAt = DateTime.UtcNow, DisplayOrder = 8 },
                    
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

            // Seed Rooms if empty
            if (!context.Rooms.Any())
            {
                // Lấy các RoomType và RoomStatus codes
                var standardRoomType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "Standard");
                var deluxeRoomType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "Deluxe");
                var vipRoomType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "VIP");
                var suiteRoomType = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "Suite");

                var availableStatus = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Available");
                var bookedStatus = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Booked");
                var occupiedStatus = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Occupied");
                var maintenanceStatus = await context.CommonCodes
                    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Maintenance");

                if (standardRoomType != null && deluxeRoomType != null && vipRoomType != null && 
                    suiteRoomType != null && availableStatus != null && bookedStatus != null && 
                    occupiedStatus != null && maintenanceStatus != null)
                {
                    var rooms = new List<Room>();

                    // Standard Rooms (Tầng 1: 101-110)
                    for (int i = 1; i <= 10; i++)
                    {
                        var statusId = i <= 6 ? availableStatus.CodeId : 
                                      i <= 8 ? bookedStatus.CodeId : 
                                      i == 9 ? occupiedStatus.CodeId : 
                                      maintenanceStatus.CodeId;

                        rooms.Add(new Room
                        {
                            RoomNumber = $"10{i}",
                            RoomTypeId = standardRoomType.CodeId,
                            BasePriceNight = 800000m,
                            BasePriceHour = 120000m,
                            StatusId = statusId,
                            Description = $"Phòng Standard {100 + i}, tầng 1, view sân vườn",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Deluxe Rooms (Tầng 2: 201-210)
                    for (int i = 1; i <= 10; i++)
                    {
                        var statusId = i <= 5 ? availableStatus.CodeId : 
                                      i <= 8 ? bookedStatus.CodeId : 
                                      occupiedStatus.CodeId;

                        rooms.Add(new Room
                        {
                            RoomNumber = $"20{i}",
                            RoomTypeId = deluxeRoomType.CodeId,
                            BasePriceNight = 1500000m,
                            BasePriceHour = 200000m,
                            StatusId = statusId,
                            Description = $"Phòng Deluxe {200 + i}, tầng 2, view thành phố",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // VIP Rooms (Tầng 3: 301-308)
                    for (int i = 1; i <= 8; i++)
                    {
                        var statusId = i <= 4 ? availableStatus.CodeId : 
                                      i <= 6 ? bookedStatus.CodeId : 
                                      occupiedStatus.CodeId;

                        rooms.Add(new Room
                        {
                            RoomNumber = $"30{i}",
                            RoomTypeId = vipRoomType.CodeId,
                            BasePriceNight = 2500000m,
                            BasePriceHour = 350000m,
                            StatusId = statusId,
                            Description = $"Phòng VIP {300 + i}, tầng 3, view biển, ban công riêng",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Suite Rooms (Tầng 4: 401-405)
                    for (int i = 1; i <= 5; i++)
                    {
                        var statusId = i <= 2 ? availableStatus.CodeId : 
                                      i <= 4 ? bookedStatus.CodeId : 
                                      occupiedStatus.CodeId;

                        rooms.Add(new Room
                        {
                            RoomNumber = $"40{i}",
                            RoomTypeId = suiteRoomType.CodeId,
                            BasePriceNight = 4000000m,
                            BasePriceHour = 550000m,
                            StatusId = statusId,
                            Description = $"Suite {400 + i}, tầng 4, phòng khách riêng, view biển panorama",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await context.Rooms.AddRangeAsync(rooms);
                    await context.SaveChangesAsync();

                    // Seed Media (Images) for Rooms
                    var media = new List<Medium>();
                    
                    // Sample images for each room type
                    var standardImages = new[]
                    {
                        "https://images.unsplash.com/photo-1611892440504-42a792e24d32?w=800",
                        "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=800",
                        "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=800"
                    };

                    var deluxeImages = new[]
                    {
                        "https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=800",
                        "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=800",
                        "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=800"
                    };

                    var vipImages = new[]
                    {
                        "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=800",
                        "https://images.unsplash.com/photo-1591088398332-8a7791972843?w=800",
                        "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?w=800",
                        "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?w=800"
                    };

                    var suiteImages = new[]
                    {
                        "https://images.unsplash.com/photo-1582719508461-905c673771fd?w=800",
                        "https://images.unsplash.com/photo-1595576508898-0ad5c879a061?w=800",
                        "https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?w=800",
                        "https://images.unsplash.com/photo-1616594039964-ae9021a400a0?w=800",
                        "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800"
                    };

                    foreach (var room in rooms)
                    {
                        string[] images;
                        string roomTypeName;

                        if (room.RoomNumber.StartsWith("1")) // Standard
                        {
                            images = standardImages;
                            roomTypeName = "Standard";
                        }
                        else if (room.RoomNumber.StartsWith("2")) // Deluxe
                        {
                            images = deluxeImages;
                            roomTypeName = "Deluxe";
                        }
                        else if (room.RoomNumber.StartsWith("3")) // VIP
                        {
                            images = vipImages;
                            roomTypeName = "VIP";
                        }
                        else // Suite
                        {
                            images = suiteImages;
                            roomTypeName = "Suite";
                        }

                        for (int i = 0; i < images.Length; i++)
                        {
                            media.Add(new Medium
                            {
                                FilePath = images[i],
                                Description = $"{roomTypeName} Room {room.RoomNumber} - Image {i + 1}",
                                DisplayOrder = i + 1,
                                CreatedAt = DateTime.UtcNow,
                                ReferenceTable = "Room",
                                ReferenceKey = room.RoomId.ToString(),
                                IsActive = true
                            });
                        }
                    }

                    await context.Media.AddRangeAsync(media);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
