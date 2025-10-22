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
                    // Status
                    new CommonCode { CodeType = "Status", CodeValue = "Hoạt động", CodeName = "Active" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Không hoạt động", CodeName = "Inactive" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã xóa", CodeName = "Deleted" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Hoàn thành", CodeName = "Completed" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Chờ xử lý", CodeName = "Pending" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đang xử lý", CodeName = "Processing" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã hủy", CodeName = "Cancelled" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đang chờ xác nhận", CodeName = "AwaitingConfirmation" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Đã xác nhận", CodeName = "Confirmed" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Status", CodeValue = "Bị từ chối", CodeName = "Rejected" , IsActive = true, CreatedAt = DateTime.UtcNow },
                    
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
                    new CommonCode { CodeType = "TaskType", CodeValue = "Dọn phòng", CodeName = "Cleaning" , Description = "Nhiệm vụ dọn dẹp vệ sinh phòng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Bảo trì", CodeName = "Maintenance" , Description = "Nhiệm vụ bảo trì sửa chữa", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Kiểm tra", CodeName = "Inspection" , Description = "Nhiệm vụ kiểm tra chất lượng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Giao hàng", CodeName = "Delivery" , Description = "Giao đồ cho khách", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TaskType", CodeValue = "Hỗ trợ khách", CodeName = "CustomerSupport" , Description = "Hỗ trợ yêu cầu khách hàng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // FeedbackType
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khiếu nại", CodeName = "Complaint" , Description = "Phản hồi về vấn đề không hài lòng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Đề xuất", CodeName = "Suggestion" , Description = "Góp ý cải thiện dịch vụ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Khen ngợi", CodeName = "Praise" , Description = "Đánh giá tích cực về dịch vụ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "FeedbackType", CodeValue = "Câu hỏi", CodeName = "Question" , Description = "Thắc mắc về dịch vụ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // NotificationType
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Hệ thống", CodeName = "System" , Description = "Thông báo từ hệ thống", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Đặt phòng", CodeName = "Booking" , Description = "Thông báo về booking", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Khuyến mãi", CodeName = "Promotion" , Description = "Thông báo ưu đãi khuyến mãi", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Thanh toán", CodeName = "Payment" , Description = "Thông báo về thanh toán", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "NotificationType", CodeValue = "Check-in/Check-out", CodeName = "CheckInOut" , Description = "Thông báo nhận/trả phòng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // BookingType
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt trực tuyến", CodeName = "Online" , Description = "Đặt phòng qua website/app", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt tại quầy", CodeName = "Walkin" , Description = "Đặt phòng trực tiếp tại lễ tân", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt qua điện thoại", CodeName = "Phone" , Description = "Đặt phòng qua hotline", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "BookingType", CodeValue = "Đặt qua đại lý", CodeName = "Agency" , Description = "Đặt phòng qua đại lý du lịch", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // PaymentStatus
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã thanh toán", CodeName = "Paid" , Description = "Đã thanh toán đầy đủ", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Chưa thanh toán", CodeName = "Unpaid" , Description = "Chưa thanh toán", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đã hoàn tiền", CodeName = "Refunded" , Description = "Đã hoàn lại tiền", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Thanh toán một phần", CodeName = "PartiallyPaid" , Description = "Đã thanh toán một phần", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentStatus", CodeValue = "Đang hoàn tiền", CodeName = "Refunding" , Description = "Đang xử lý hoàn tiền", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // DepositStatus
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Đã đặt cọc", CodeName = "Paid" , Description = "Đã thanh toán tiền đặt cọc", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Chưa đặt cọc", CodeName = "Unpaid" , Description = "Chưa thanh toán tiền cọc", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "DepositStatus", CodeValue = "Đã hoàn cọc", CodeName = "Refunded" , Description = "Đã hoàn lại tiền cọc", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // PaymentMethod
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Tiền mặt", CodeName = "Cash" , Description = "Thanh toán bằng tiền mặt", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Thẻ ngân hàng", CodeName = "Card" , Description = "Thanh toán bằng thẻ ATM/Credit", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Chuyển khoản", CodeName = "Bank" , Description = "Chuyển khoản ngân hàng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "Ví điện tử", CodeName = "EWallet" , Description = "Ví điện tử (Momo, ZaloPay, VNPay)", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "PaymentMethod", CodeValue = "PayOS", CodeName = "PayOS" , Description = "Cổng thanh toán PayOS", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // TransactionStatus
                    new CommonCode { CodeType = "TransactionStatus", CodeValue = "Đang xử lý", CodeName = "Pending" , Description = "Giao dịch đang được xử lý", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TransactionStatus", CodeValue = "Hoàn thành", CodeName = "Completed" , Description = "Giao dịch thành công", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TransactionStatus", CodeValue = "Thất bại", CodeName = "Failed" , Description = "Giao dịch thất bại", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TransactionStatus", CodeValue = "Đã hủy", CodeName = "Cancelled" , Description = "Giao dịch đã bị hủy", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "TransactionStatus", CodeValue = "Đang hoàn tiền", CodeName = "Refunding" , Description = "Đang xử lý hoàn tiền", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // RoomStatus
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Trống", CodeName = "Available" , Description = "Phòng trống, sẵn sàng cho thuê", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đã đặt", CodeName = "Booked" , Description = "Phòng đã được đặt trước", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đang sử dụng", CodeName = "Occupied" , Description = "Phòng đang có khách ở", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Đang dọn dẹp", CodeName = "Cleaning" , Description = "Phòng đang được dọn dẹp", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Bảo trì", CodeName = "Maintenance" , Description = "Phòng đang bảo trì, không cho thuê", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "RoomStatus", CodeValue = "Chờ kiểm tra", CodeName = "PendingInspection" , Description = "Phòng chờ kiểm tra chất lượng", IsActive = true, CreatedAt = DateTime.UtcNow },
                    
                    // Priority Level (cho Task, Feedback)
                    new CommonCode { CodeType = "Priority", CodeValue = "Thấp", CodeName = "Low" , Description = "Mức ưu tiên thấp", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Priority", CodeValue = "Trung bình", CodeName = "Medium" , Description = "Mức ưu tiên trung bình", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Priority", CodeValue = "Cao", CodeName = "High" , Description = "Mức ưu tiên cao", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new CommonCode { CodeType = "Priority", CodeValue = "Khẩn cấp", CodeName = "Urgent" , Description = "Mức ưu tiên khẩn cấp", IsActive = true, CreatedAt = DateTime.UtcNow }
                });
                await context.SaveChangesAsync();
            }

            // Seed RoomType if empty (Bảng riêng biệt, không dùng CommonCode)
            if (!context.Set<RoomType>().Any())
            {
                var roomTypes = new List<RoomType>
                {
                    new RoomType
                    {
                        TypeName = "Phòng Tiêu Chuẩn",
                        TypeCode = "STD",
                        Description = "Phòng tiêu chuẩn 25m² với đầy đủ tiện nghi cơ bản, giường Queen size, phù hợp cho 1-2 người. View sân vườn hoặc thành phố.",
                        BasePriceNight = 800000m,
                        MaxOccupancy = 2,
                        RoomSize = 25m,
                        NumberOfBeds = 1,
                        BedType = "Queen",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new RoomType
                    {
                        TypeName = "Phòng Cao Cấp",
                        TypeCode = "DLX",
                        Description = "Phòng Deluxe 35m² rộng rãi với 2 giường Queen, view thành phố tuyệt đẹp, không gian hiện đại sang trọng, phù hợp cho 2-3 người.",
                        BasePriceNight = 1500000m,
                        MaxOccupancy = 3,
                        RoomSize = 35m,
                        NumberOfBeds = 2,
                        BedType = "Queen",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new RoomType
                    {
                        TypeName = "Phòng VIP",
                        TypeCode = "VIP",
                        Description = "Phòng VIP 45m² cao cấp với giường King size, ban công riêng view biển tuyệt đẹp, bồn tắm nằm sang trọng, phù hợp cho 2-4 người. Đầy đủ tiện nghi 5 sao.",
                        BasePriceNight = 2500000m,
                        MaxOccupancy = 4,
                        RoomSize = 45m,
                        NumberOfBeds = 2,
                        BedType = "King",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new RoomType
                    {
                        TypeName = "Suite Sang Trọng",
                        TypeCode = "SUT",
                        Description = "Suite 70m² cực kỳ sang trọng với phòng khách riêng biệt, 3 giường King & Queen, view biển panorama 180 độ, phù hợp cho gia đình 4-6 người. Bao gồm minibar, máy pha cà phê cao cấp và dịch vụ butler.",
                        BasePriceNight = 4000000m,
                        MaxOccupancy = 6,
                        RoomSize = 70m,
                        NumberOfBeds = 3,
                        BedType = "King & Queen",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Set<RoomType>().AddRangeAsync(roomTypes);
                await context.SaveChangesAsync();

                // Add Media (Images) for each RoomType
                var roomTypeMedia = new List<Medium>();
                
                // Standard Room Images
                var standardRoomType = roomTypes[0];
                roomTypeMedia.AddRange(new[]
                {
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?w=1200&q=80",
                        Description = "Phòng Tiêu Chuẩn - View tổng quan",
                        DisplayOrder = 1,
                        ReferenceTable = "RoomType",
                        ReferenceKey = standardRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1590490360182-c33d57733427?w=1200&q=80",
                        Description = "Phòng Tiêu Chuẩn - Giường ngủ",
                        DisplayOrder = 2,
                        ReferenceTable = "RoomType",
                        ReferenceKey = standardRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=1200&q=80",
                        Description = "Phòng Tiêu Chuẩn - Phòng tắm",
                        DisplayOrder = 3,
                        ReferenceTable = "RoomType",
                        ReferenceKey = standardRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });

                // Deluxe Room Images
                var deluxeRoomType = roomTypes[1];
                roomTypeMedia.AddRange(new[]
                {
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?w=1200&q=80",
                        Description = "Phòng Cao Cấp - View tổng quan",
                        DisplayOrder = 1,
                        ReferenceTable = "RoomType",
                        ReferenceKey = deluxeRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=1200&q=80",
                        Description = "Phòng Cao Cấp - Khu vực giường",
                        DisplayOrder = 2,
                        ReferenceTable = "RoomType",
                        ReferenceKey = deluxeRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?w=1200&q=80",
                        Description = "Phòng Cao Cấp - View thành phố",
                        DisplayOrder = 3,
                        ReferenceTable = "RoomType",
                        ReferenceKey = deluxeRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?w=1200&q=80",
                        Description = "Phòng Cao Cấp - Phòng tắm sang trọng",
                        DisplayOrder = 4,
                        ReferenceTable = "RoomType",
                        ReferenceKey = deluxeRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });

                // VIP Room Images
                var vipRoomType = roomTypes[2];
                roomTypeMedia.AddRange(new[]
                {
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1591088398332-8a7791972843?w=1200&q=80",
                        Description = "Phòng VIP - View biển tuyệt đẹp",
                        DisplayOrder = 1,
                        ReferenceTable = "RoomType",
                        ReferenceKey = vipRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1596394516093-501ba68a0ba6?w=1200&q=80",
                        Description = "Phòng VIP - Giường King size cao cấp",
                        DisplayOrder = 2,
                        ReferenceTable = "RoomType",
                        ReferenceKey = vipRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?w=1200&q=80",
                        Description = "Phòng VIP - Ban công riêng view biển",
                        DisplayOrder = 3,
                        ReferenceTable = "RoomType",
                        ReferenceKey = vipRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1583847268964-b28dc8f51f92?w=1200&q=80",
                        Description = "Phòng VIP - Bồn tắm nằm sang trọng",
                        DisplayOrder = 4,
                        ReferenceTable = "RoomType",
                        ReferenceKey = vipRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1584132967334-10e028bd69f7?w=1200&q=80",
                        Description = "Phòng VIP - Khu vực phòng khách",
                        DisplayOrder = 5,
                        ReferenceTable = "RoomType",
                        ReferenceKey = vipRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });

                // Suite Room Images
                var suiteRoomType = roomTypes[3];
                roomTypeMedia.AddRange(new[]
                {
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1582719508461-905c673771fd?w=1200&q=80",
                        Description = "Suite - Phòng khách sang trọng",
                        DisplayOrder = 1,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1595576508898-0ad5c879a061?w=1200&q=80",
                        Description = "Suite - View biển panorama",
                        DisplayOrder = 2,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?w=1200&q=80",
                        Description = "Suite - Phòng ngủ chính",
                        DisplayOrder = 3,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1616594039964-ae9021a400a0?w=1200&q=80",
                        Description = "Suite - Phòng tắm cao cấp",
                        DisplayOrder = 4,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1590381105924-c72589b9ef3f?w=1200&q=80",
                        Description = "Suite - Ban công view biển rộng",
                        DisplayOrder = 5,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Medium
                    {
                        FilePath = "https://images.unsplash.com/photo-1582719508461-905c673771fd?w=1200&q=80",
                        Description = "Suite - Phòng khách sang trọng",
                        DisplayOrder = 6,
                        ReferenceTable = "RoomType",
                        ReferenceKey = suiteRoomType.RoomTypeId.ToString(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });

                await context.Media.AddRangeAsync(roomTypeMedia);
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
                // Lấy các RoomType và RoomStatus
                var standardRoomType = await context.Set<RoomType>()
                    .FirstOrDefaultAsync(rt => rt.TypeCode == "STD");
                var deluxeRoomType = await context.Set<RoomType>()
                    .FirstOrDefaultAsync(rt => rt.TypeCode == "DLX");
                var vipRoomType = await context.Set<RoomType>()
                    .FirstOrDefaultAsync(rt => rt.TypeCode == "VIP");
                var suiteRoomType = await context.Set<RoomType>()
                    .FirstOrDefaultAsync(rt => rt.TypeCode == "SUT");

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
                            RoomName = $"Standard-10{i}",
                            RoomTypeId = standardRoomType.RoomTypeId,
                            StatusId = statusId,
                            Description = $"Phòng Standard tầng 1 số {100 + i}, view sân vườn",
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
                            RoomName = $"Deluxe-20{i}",
                            RoomTypeId = deluxeRoomType.RoomTypeId,
                            StatusId = statusId,
                            Description = $"Phòng Deluxe tầng 2 số {200 + i}, view thành phố",
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
                            RoomName = $"VIP-30{i}",
                            RoomTypeId = vipRoomType.RoomTypeId,
                            StatusId = statusId,
                            Description = $"Phòng VIP tầng 3 số {300 + i}, view biển, ban công riêng",
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
                            RoomName = $"Suite-40{i}",
                            RoomTypeId = suiteRoomType.RoomTypeId,
                            StatusId = statusId,
                            Description = $"Suite tầng 4 số {400 + i}, phòng khách riêng, view biển panorama",
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

                        if (room.RoomName.StartsWith("Standard")) // Standard
                        {
                            images = standardImages;
                            roomTypeName = "Standard";
                        }
                        else if (room.RoomName.StartsWith("Deluxe")) // Deluxe
                        {
                            images = deluxeImages;
                            roomTypeName = "Deluxe";
                        }
                        else if (room.RoomName.StartsWith("VIP")) // VIP
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
                                Description = $"{roomTypeName} Room {room.RoomName} - Image {i + 1}",
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

            // Seed Amenities if empty
            if (!context.Amenities.Any())
            {
                var amenities = new List<Amenity>
                {
                    // Common Amenities - Tiện nghi cơ bản (miễn phí, có sẵn)
                    new Amenity
                    {
                        AmenityName = "WiFi miễn phí",
                        Description = "Kết nối WiFi tốc độ cao miễn phí trong phòng",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Điều hòa nhiệt độ",
                        Description = "Hệ thống điều hòa không khí tự động",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "TV màn hình phẳng",
                        Description = "TV LED với các kênh truyền hình cáp",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Tủ lạnh mini",
                        Description = "Tủ lạnh nhỏ để bảo quản đồ uống",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Két an toàn",
                        Description = "Két sắt điện tử để bảo quản tài sản",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Máy sấy tóc",
                        Description = "Máy sấy tóc công suất cao",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Bộ đồ dùng phòng tắm",
                        Description = "Dầu gội, sữa tắm, xà phòng miễn phí",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Ấm đun nước",
                        Description = "Ấm đun nước điện với trà/cà phê miễn phí",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Máy lạnh",
                        Description = "Máy lạnh điều chỉnh nhiệt độ",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Bàn làm việc",
                        Description = "Bàn và ghế làm việc",
                        AmenityType = "Common",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },

                    // Additional Amenities - Tiện nghi bổ sung (theo yêu cầu, có thể tính phí)
                    new Amenity
                    {
                        AmenityName = "Bồn tắm nằm",
                        Description = "Bồn tắm nằm thư giãn",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Máy pha cà phê cao cấp",
                        Description = "Máy pha cà phê Nespresso với capsule",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Minibar",
                        Description = "Tủ minibar với đồ uống và snack",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Máy lọc không khí",
                        Description = "Hệ thống lọc không khí và tạo ẩm",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Áo choàng tắm",
                        Description = "Áo choàng tắm và dép đi trong phòng",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Loa Bluetooth",
                        Description = "Loa Bluetooth cao cấp",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Bàn ủi & bàn ủi đồ",
                        Description = "Bàn ủi và bàn ủi quần áo",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Dịch vụ giặt là",
                        Description = "Dịch vụ giặt ủi quần áo",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Dịch vụ phòng 24/7",
                        Description = "Dịch vụ room service 24 giờ",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Amenity
                    {
                        AmenityName = "Ban công riêng",
                        Description = "Ban công với view đẹp",
                        AmenityType = "Additional",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await context.Amenities.AddRangeAsync(amenities);
                await context.SaveChangesAsync();
            }

            // Seed Holidays if empty
            if (!context.Holidays.Any())
            {
                var holidays = new List<Holiday>
                {
                    new Holiday
                    {
                        Name = "Tết Nguyên Đán 2025",
                        StartDate = new DateTime(2025, 1, 28),
                        EndDate = new DateTime(2025, 2, 3),
                        Description = "Tết Âm lịch - Ngày lễ truyền thống của Việt Nam",
                        IsActive = true
                    },
                    new Holiday
                    {
                        Name = "Lễ 30/4 - 1/5",
                        StartDate = new DateTime(2025, 4, 30),
                        EndDate = new DateTime(2025, 5, 3),
                        Description = "Ngày giải phóng miền Nam và Quốc tế Lao động",
                        IsActive = true
                    },
                    new Holiday
                    {
                        Name = "Quốc Khánh 2/9",
                        StartDate = new DateTime(2025, 9, 1),
                        EndDate = new DateTime(2025, 9, 3),
                        Description = "Ngày Quốc khánh nước Cộng hòa xã hội chủ nghĩa Việt Nam",
                        IsActive = true
                    }
                };

                await context.Holidays.AddRangeAsync(holidays);
                await context.SaveChangesAsync();

                // Seed HolidayPricing - Điều chỉnh giá theo đêm (KHÔNG TÍNH THEO GIỜ)
                // Lấy các phòng để áp dụng giá ngày lễ
                var room101 = await context.Rooms.FirstOrDefaultAsync(r => r.RoomName.Contains("101"));
                var room102 = await context.Rooms.FirstOrDefaultAsync(r => r.RoomName.Contains("102"));
                var room201 = await context.Rooms.FirstOrDefaultAsync(r => r.RoomName.Contains("201"));

                var tetHoliday = holidays.FirstOrDefault(h => h.Name == "Tết Nguyên Đán 2025");
                var labor30_4Holiday = holidays.FirstOrDefault(h => h.Name == "Lễ 30/4 - 1/5");
                var nationalDayHoliday = holidays.FirstOrDefault(h => h.Name == "Quốc Khánh 2/9");

                if (tetHoliday != null && labor30_4Holiday != null && nationalDayHoliday != null)
                {
                    var holidayPricings = new List<HolidayPricing>();

                    // Tết Nguyên Đán - Tăng giá cho phòng trong dịp Tết (CHỈ TÍNH THEO ĐÊM)
                    if (room101 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = tetHoliday.HolidayId,
                            RoomId = room101.RoomId,
                            PriceAdjustment = 300000m, // Tăng 300k/đêm (800k -> 1,100k)
                            StartDate = tetHoliday.StartDate,
                            EndDate = tetHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room102 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = tetHoliday.HolidayId,
                            RoomId = room102.RoomId,
                            PriceAdjustment = 300000m, // Tăng 300k/đêm (800k -> 1,100k)
                            StartDate = tetHoliday.StartDate,
                            EndDate = tetHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room201 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = tetHoliday.HolidayId,
                            RoomId = room201.RoomId,
                            PriceAdjustment = 500000m, // Tăng 500k/đêm (1,500k -> 2,000k)
                            StartDate = tetHoliday.StartDate,
                            EndDate = tetHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    // Lễ 30/4 - 1/5 - Tăng giá trong dịp lễ (CHỈ TÍNH THEO ĐÊM)
                    if (room101 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = labor30_4Holiday.HolidayId,
                            RoomId = room101.RoomId,
                            PriceAdjustment = 200000m, // Tăng 200k/đêm (800k -> 1,000k)
                            StartDate = labor30_4Holiday.StartDate,
                            EndDate = labor30_4Holiday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room102 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = labor30_4Holiday.HolidayId,
                            RoomId = room102.RoomId,
                            PriceAdjustment = 200000m, // Tăng 200k/đêm (800k -> 1,000k)
                            StartDate = labor30_4Holiday.StartDate,
                            EndDate = labor30_4Holiday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room201 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = labor30_4Holiday.HolidayId,
                            RoomId = room201.RoomId,
                            PriceAdjustment = 300000m, // Tăng 300k/đêm (1,500k -> 1,800k)
                            StartDate = labor30_4Holiday.StartDate,
                            EndDate = labor30_4Holiday.EndDate,
                            IsActive = true
                        });
                    }

                    // Quốc Khánh 2/9 - Tăng giá trong dịp lễ Quốc Khánh (CHỈ TÍNH THEO ĐÊM)
                    if (room101 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = nationalDayHoliday.HolidayId,
                            RoomId = room101.RoomId,
                            PriceAdjustment = 200000m, // Tăng 200k/đêm (800k -> 1,000k)
                            StartDate = nationalDayHoliday.StartDate,
                            EndDate = nationalDayHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room102 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = nationalDayHoliday.HolidayId,
                            RoomId = room102.RoomId,
                            PriceAdjustment = 200000m, // Tăng 200k/đêm (800k -> 1,000k)
                            StartDate = nationalDayHoliday.StartDate,
                            EndDate = nationalDayHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    if (room201 != null)
                    {
                        holidayPricings.Add(new HolidayPricing
                        {
                            HolidayId = nationalDayHoliday.HolidayId,
                            RoomId = room201.RoomId,
                            PriceAdjustment = 300000m, // Tăng 300k/đêm (1,500k -> 1,800k)
                            StartDate = nationalDayHoliday.StartDate,
                            EndDate = nationalDayHoliday.EndDate,
                            IsActive = true
                        });
                    }

                    await context.HolidayPricings.AddRangeAsync(holidayPricings);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
