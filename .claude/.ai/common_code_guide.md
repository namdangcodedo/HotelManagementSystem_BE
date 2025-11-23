# CommonCode Guide for Hotel Management System

## Nguyên tắc sử dụng CommonCode

### ⚠️ NGUYÊN TẮC VÀNG
- **KHÔNG BAO GIỜ hardcode ID của Status/Type!**
- **LUÔN LUÔN lấy ID từ CommonCode sử dụng CodeName và CodeType**

### ❌ SAI - Hardcode ID
```csharp
// TUYỆT ĐỐI KHÔNG LÀM NHƯ VẦY!
room.StatusId = 5;
booking.PaymentStatusId = 2;
```

### ✅ ĐÚNG - Lấy từ CommonCode
```csharp
// Lấy ID từ CommonCode bằng CodeName và CodeType
var activeStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "Status" && c.CodeName == "Active");
room.StatusId = activeStatus?.CodeId ?? 0;
```

## Bảng tra cứu CommonCode

### 1. Status
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Active` | Hoạt động | Trạng thái hoạt động |
| `Inactive` | Không hoạt động | Trạng thái không hoạt động |
| `Deleted` | Đã xóa | Đã xóa mềm |
| `Completed` | Hoàn thành | Đã hoàn thành |
| `Pending` | Chờ xử lý | Đang chờ xử lý |
| `Processing` | Đang xử lý | Đang trong quá trình xử lý |
| `Cancelled` | Đã hủy | Đã hủy bỏ |
| `AwaitingConfirmation` | Đang chờ xác nhận | Chờ xác nhận |
| `Confirmed` | Đã xác nhận | Đã xác nhận |
| `Rejected` | Bị từ chối | Bị từ chối |

### 2. EmployeeType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Admin` | Quản trị viên | Quản trị viên hệ thống |
| `Manager` | Quản lý | Quản lý khách sạn |
| `Receptionist` | Lễ tân | Nhân viên lễ tân |
| `Housekeeper` | Nhân viên dọn phòng | Nhân viên dọn dẹp phòng |
| `Technician` | Kỹ thuật viên | Kỹ thuật viên bảo trì |
| `Security` | Bảo vệ | Nhân viên bảo vệ |
| `Chef` | Đầu bếp | Đầu bếp nhà hàng |
| `Waiter` | Nhân viên phục vụ | Nhân viên phục vụ nhà hàng |

### 3. TaskType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Cleaning` | Dọn phòng | Nhiệm vụ dọn dẹp vệ sinh phòng |
| `Maintenance` | Bảo trì | Nhiệm vụ bảo trì sửa chữa |
| `Inspection` | Kiểm tra | Nhiệm vụ kiểm tra chất lượng |
| `Delivery` | Giao hàng | Giao đồ cho khách |
| `CustomerSupport` | Hỗ trợ khách | Hỗ trợ yêu cầu khách hàng |

### 4. FeedbackType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Complaint` | Khiếu nại | Phản hồi về vấn đề không hài lòng |
| `Suggestion` | Đề xuất | Góp ý cải thiện dịch vụ |
| `Praise` | Khen ngợi | Đánh giá tích cực về dịch vụ |
| `Question` | Câu hỏi | Thắc mắc về dịch vụ |

### 5. NotificationType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `System` | Hệ thống | Thông báo từ hệ thống |
| `Booking` | Đặt phòng | Thông báo về booking |
| `Promotion` | Khuyến mãi | Thông báo ưu đãi khuyến mãi |
| `Payment` | Thanh toán | Thông báo về thanh toán |
| `CheckInOut` | Check-in/Check-out | Thông báo nhận/trả phòng |

### 6. BookingType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Online` | Đặt trực tuyến | Đặt phòng qua website/app |
| `Walkin` | Đặt tại quầy | Đặt phòng trực tiếp tại lễ tân |
| `Phone` | Đặt qua điện thoại | Đặt phòng qua hotline |
| `Agency` | Đặt qua đại lý | Đặt phòng qua đại lý du lịch |

### 7. PaymentStatus
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Paid` | Đã thanh toán | Đã thanh toán đầy đủ |
| `Unpaid` | Chưa thanh toán | Chưa thanh toán |
| `Refunded` | Đã hoàn tiền | Đã hoàn lại tiền |
| `PartiallyPaid` | Thanh toán một phần | Đã thanh toán một phần |
| `Refunding` | Đang hoàn tiền | Đang xử lý hoàn tiền |

### 8. DepositStatus
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Paid` | Đã đặt cọc | Đã thanh toán tiền đặt cọc |
| `Unpaid` | Chưa đặt cọc | Chưa thanh toán tiền cọc |
| `Refunded` | Đã hoàn cọc | Đã hoàn lại tiền cọc |

### 9. PaymentMethod
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Cash` | Tiền mặt | Thanh toán bằng tiền mặt |
| `Card` | Thẻ ngân hàng | Thanh toán bằng thẻ ATM/Credit |
| `Bank` | Chuyển khoản | Chuyển khoản ngân hàng |
| `EWallet` | Ví điện tử | Ví điện tử (Momo, ZaloPay, VNPay) |
| `PayOS` | PayOS | Cổng thanh toán PayOS |

## Cách tìm kiếm CommonCode trong code

### Tìm kiếm theo CodeType
```csharp
// Lấy tất cả status
var statuses = await _unitOfWork.CommonCodes
    .Where(c => c.CodeType == "Status" && c.IsActive)
    .ToListAsync();

// Lấy theo CodeName cụ thể
var activeStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "Status" && c.CodeName == "Active");
```

### Sử dụng trong Service
```csharp
public async Task<ResultModel> CreateBookingAsync(CreateBookingRequest request)
{
    // Lấy PaymentStatus Unpaid
    var unpaidStatus = await _unitOfWork.CommonCodes
        .FirstOrDefaultAsync(c => c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid");

    var booking = new Booking
    {
        // ... other properties
        PaymentStatusId = unpaidStatus?.CodeId ?? 0
    };

    await _unitOfWork.Bookings.AddAsync(booking);
    await _unitOfWork.SaveChangesAsync();

    return new ResultModel { IsSuccess = true, Message = "Booking created" };
}
```

---
Hướng dẫn này giúp Claude AI hiểu cách sử dụng CommonCode đúng cách, tránh hardcode và đảm bảo tính linh hoạt của hệ thống.
