# CommonCode Reference Guide

## ⚠️ NGUYÊN TẮC VÀNG

> **KHÔNG BAO GIỜ hardcode ID của Status/Type!**
> 
> **LUÔN LUÔN lấy ID từ CommonCode sử dụng CodeName**

## Cách sử dụng CommonCode đúng

### ❌ SAI - Hardcode ID
```csharp
// TUYỆT ĐỐI KHÔNG LÀM NHƯ VẦY!
room.StatusId = 5;
booking.PaymentStatusId = 2;
booking.BookingTypeId = 1;
```

### ✅ ĐÚNG - Lấy từ CommonCode
```csharp
// Lấy ID từ CommonCode bằng CodeName
var availableStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Available");
room.StatusId = availableStatus?.CodeId ?? 0;

var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
booking.PaymentStatusId = unpaidStatus?.CodeId;
```

## Bảng tra cứu CommonCode

### 1. RoomType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Standard` | Phòng tiêu chuẩn | Phòng cơ bản |
| `Deluxe` | Phòng cao cấp | Phòng nâng cấp |
| `VIP` | Phòng VIP | Phòng VIP |
| `Suite` | Suite | Phòng suite cao cấp |

**Ví dụ:**
```csharp
var standardRoomType = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "Standard");
```

---

### 2. RoomStatus
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Available` | Trống | Phòng trống, sẵn sàng cho thuê |
| `Booked` | Đã đặt | Phòng đã được đặt |
| `Occupied` | Đang sử dụng | Phòng đang có khách |
| `Cleaning` | Đang dọn dẹp | Phòng đang được dọn |
| `Maintenance` | Bảo trì | Phòng đang bảo trì |

**Ví dụ:**
```csharp
var availableStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Available");
```

---

### 3. Status (General)
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Active` | Hoạt động | Đang hoạt động |
| `Inactive` | Không hoạt động | Tạm ngưng |
| `Deleted` | Đã xóa | Đã bị xóa |
| `Completed` | Hoàn thành | Đã hoàn thành |
| `Pending` | Chờ xử lý | Đang chờ |
| `Processing` | Đang xử lý | Đang xử lý |
| `Cancelled` | Đã hủy | Đã bị hủy |

**Ví dụ:**
```csharp
var activeStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "Status" && c.CodeName == "Active");
```

---

### 4. PaymentStatus
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Paid` | Đã thanh toán | Đã thanh toán đầy đủ |
| `Unpaid` | Chưa thanh toán | Chưa thanh toán |
| `Refunded` | Đã hoàn tiền | Đã hoàn lại tiền |
| `PartiallyPaid` | Thanh toán một phần | Thanh toán một phần |

**Ví dụ:**
```csharp
var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
```

---

### 5. DepositStatus
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Paid` | Đã đặt cọc | Đã đặt cọc |
| `Unpaid` | Chưa đặt cọc | Chưa đặt cọc |

**Ví dụ:**
```csharp
var paidDepositStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "DepositStatus" && c.CodeName == "Paid")).FirstOrDefault();
```

---

### 6. BookingType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Online` | Đặt trực tuyến | Đặt qua website/app |
| `Walkin` | Đặt tại quầy | Đặt trực tiếp tại quầy |

**Ví dụ:**
```csharp
var onlineBookingType = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "BookingType" && c.CodeName == "Online")).FirstOrDefault();
```

---

### 7. PaymentMethod
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Cash` | Tiền mặt | Thanh toán bằng tiền mặt |
| `Card` | Thẻ ngân hàng | Thanh toán bằng thẻ |
| `Bank` | Chuyển khoản | Chuyển khoản ngân hàng |
| `EWallet` | Ví điện tử | Thanh toán qua ví điện tử |

**Ví dụ:**
```csharp
var eWalletMethod = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "PaymentMethod" && c.CodeName == "EWallet")).FirstOrDefault();
```

---

### 8. EmployeeType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Admin` | Quản trị viên | Quản trị viên hệ thống |
| `Manager` | Quản lý | Quản lý khách sạn |
| `Receptionist` | Lễ tân | Nhân viên lễ tân |
| `Housekeeper` | Nhân viên dọn phòng | Nhân viên dọn dẹp |
| `Technician` | Kỹ thuật viên | Kỹ thuật viên bảo trì |
| `Security` | Bảo vệ | Nhân viên bảo vệ |
| `Chef` | Đầu bếp | Đầu bếp nhà hàng |
| `Waiter` | Nhân viên phục vụ | Nhân viên phục vụ |

**Ví dụ:**
```csharp
var receptionistType = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "EmployeeType" && c.CodeName == "Receptionist");
```

---

### 9. TaskType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Cleaning` | Dọn phòng | Công việc dọn phòng |
| `Maintenance` | Bảo trì | Công việc bảo trì |
| `Inspection` | Kiểm tra | Công việc kiểm tra |

**Ví dụ:**
```csharp
var cleaningTask = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "TaskType" && c.CodeName == "Cleaning");
```

---

### 10. FeedbackType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `Complaint` | Khiếu nại | Phản hồi khiếu nại |
| `Suggestion` | Đề xuất | Phản hồi đề xuất |
| `Praise` | Khen ngợi | Phản hồi khen ngợi |

**Ví dụ:**
```csharp
var complaintType = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "FeedbackType" && c.CodeName == "Complaint");
```

---

### 11. NotificationType
| CodeName | CodeValue (Vietnamese) | Description |
|----------|------------------------|-------------|
| `System` | Hệ thống | Thông báo hệ thống |
| `Booking` | Đặt phòng | Thông báo đặt phòng |
| `Promotion` | Khuyến mãi | Thông báo khuyến mãi |

**Ví dụ:**
```csharp
var bookingNotif = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "NotificationType" && c.CodeName == "Booking");
```

---

## Template Code để sử dụng

### Template 1: Get Single CommonCode
```csharp
// Method: FirstOrDefaultAsync
var codeItem = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "CodeType" && c.CodeName == "CodeName");

if (codeItem == null)
{
    return BadRequest("Invalid code");
}

entity.FieldId = codeItem.CodeId;
```

### Template 2: Get CommonCode with FindAsync
```csharp
// Method: FindAsync (returns IEnumerable)
var codeItem = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "CodeType" && c.CodeName == "CodeName")).FirstOrDefault();

if (codeItem != null)
{
    entity.FieldId = codeItem.CodeId;
}
```

### Template 3: Get Multiple CommonCodes
```csharp
var statuses = new Dictionary<string, int?>();

var available = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Available");
statuses["Available"] = available?.CodeId;

var booked = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Booked");
statuses["Booked"] = booked?.CodeId;
```

## Helper Service để tái sử dụng

```csharp
public class CommonCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private Dictionary<string, int> _codeCache = new();

    public CommonCodeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int?> GetCodeIdAsync(string codeType, string codeName)
    {
        var key = $"{codeType}:{codeName}";
        
        if (_codeCache.ContainsKey(key))
        {
            return _codeCache[key];
        }

        var code = await _unitOfWork.CommonCodes
            .FirstOrDefaultAsync(c => c.CodeType == codeType && c.CodeName == codeName);

        if (code != null)
        {
            _codeCache[key] = code.CodeId;
            return code.CodeId;
        }

        return null;
    }

    // Shortcut methods
    public Task<int?> GetRoomStatusId(string statusName) 
        => GetCodeIdAsync("RoomStatus", statusName);

    public Task<int?> GetPaymentStatusId(string statusName) 
        => GetCodeIdAsync("PaymentStatus", statusName);

    public Task<int?> GetBookingTypeId(string typeName) 
        => GetCodeIdAsync("BookingType", typeName);
}
```

## Checklist khi code

- [ ] Đã import `using Microsoft.EntityFrameworkCore;`
- [ ] Đã inject `IUnitOfWork _unitOfWork`
- [ ] Đã sử dụng `CodeType` và `CodeName` đúng
- [ ] Đã kiểm tra `null` trước khi gán
- [ ] Đã xử lý case không tìm thấy code
- [ ] Không hardcode bất kỳ ID nào

## Tài liệu tham khảo

- **Seeding Data**: `/AppBackend.ApiCore/Extensions/SeedingData.cs`
- **CommonCode Model**: `/AppBackend.BusinessObjects/Models/CommonCode.cs`
- **CommonCode Repository**: `/AppBackend.Repositories/Repositories/CommonCodeRepo/`

---

**Cập nhật lần cuối**: 2025-01-19  
**Version**: 1.0

