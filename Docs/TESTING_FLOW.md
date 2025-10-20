# Luồng Test và Best Practices

## ⚠️ QUAN TRỌNG: Lấy Status và Type từ CommonCode

### Nguyên tắc quan trọng

**TẤT CẢ các loại Status, Type, và các giá trị danh mục khác PHẢI lấy từ bảng `CommonCode`**

Không được hardcode ID trực tiếp! Phải sử dụng repository của CommonCode để lấy ID từ `CodeName` theo seeding data.

### Các trường hợp phổ biến

#### 1. Room Status
```csharp
// ❌ SAI - Không hardcode ID
var roomStatusId = 5; // Trống

// ✅ ĐÚNG - Lấy từ CommonCode
var availableStatus = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomStatus" && c.CodeName == "Available");
var roomStatusId = availableStatus?.CodeId ?? 0;
```

#### 2. Payment Status
```csharp
// ❌ SAI
booking.PaymentStatusId = 2; // Hardcode

// ✅ ĐÚNG
var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();
if (unpaidStatus != null) 
    booking.PaymentStatusId = unpaidStatus.CodeId;
```

#### 3. Booking Type
```csharp
// ❌ SAI
booking.BookingTypeId = 1; // Hardcode

// ✅ ĐÚNG
var onlineBookingType = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "BookingType" && c.CodeName == "Online")).FirstOrDefault();
if (onlineBookingType != null) 
    booking.BookingTypeId = onlineBookingType.CodeId;
```

#### 4. Room Type
```csharp
// ❌ SAI
var roomTypeId = 1; // Standard

// ✅ ĐÚNG
var standardRoomType = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "RoomType" && c.CodeName == "Standard");
var roomTypeId = standardRoomType?.CodeId ?? 0;
```

## Danh sách các CodeType trong hệ thống

### 1. RoomType
- `Standard` - Phòng tiêu chuẩn
- `Deluxe` - Phòng cao cấp
- `VIP` - Phòng VIP
- `Suite` - Suite

### 2. RoomStatus
- `Available` - Trống
- `Booked` - Đã đặt
- `Occupied` - Đang sử dụng
- `Cleaning` - Đang dọn dẹp
- `Maintenance` - Bảo trì

### 3. Status (General)
- `Active` - Hoạt động
- `Inactive` - Không hoạt động
- `Deleted` - Đã xóa
- `Completed` - Hoàn thành
- `Pending` - Chờ xử lý
- `Processing` - Đang xử lý
- `Cancelled` - Đã hủy

### 4. PaymentStatus
- `Paid` - Đã thanh toán
- `Unpaid` - Chưa thanh toán
- `Refunded` - Đã hoàn tiền
- `PartiallyPaid` - Thanh toán một phần

### 5. DepositStatus
- `Paid` - Đã đặt cọc
- `Unpaid` - Chưa đặt cọc

### 6. BookingType
- `Online` - Đặt trực tuyến
- `Walkin` - Đặt tại quầy

### 7. PaymentMethod
- `Cash` - Tiền mặt
- `Card` - Thẻ ngân hàng
- `Bank` - Chuyển khoản
- `EWallet` - Ví điện tử

### 8. EmployeeType
- `Admin` - Quản trị viên
- `Manager` - Quản lý
- `Receptionist` - Lễ tân
- `Housekeeper` - Nhân viên dọn phòng
- `Technician` - Kỹ thuật viên
- `Security` - Bảo vệ
- `Chef` - Đầu bếp
- `Waiter` - Nhân viên phục vụ

### 9. TaskType
- `Cleaning` - Dọn phòng
- `Maintenance` - Bảo trì
- `Inspection` - Kiểm tra

### 10. FeedbackType
- `Complaint` - Khiếu nại
- `Suggestion` - Đề xuất
- `Praise` - Khen ngợi

### 11. NotificationType
- `System` - Hệ thống
- `Booking` - Đặt phòng
- `Promotion` - Khuyến mãi

## Luồng Test Booking với Holiday Pricing

### Test Case 1: Booking thường (không có ngày lễ)

```http
### 1. Tạo booking cho 2 đêm (ngày thường)
POST https://localhost:7000/api/booking/create
Content-Type: application/json
Authorization: Bearer {{access_token}}

{
  "customerId": 1,
  "roomIds": [1, 2],  // Room 101, 102
  "checkInDate": "2025-03-15T14:00:00",
  "checkOutDate": "2025-03-17T12:00:00",
  "bookingType": "Online",
  "specialRequests": "Late check-in please"
}

# Kết quả mong đợi:
# - Room 101: 800k/đêm × 2 đêm = 1,600k
# - Room 102: 800k/đêm × 2 đêm = 1,600k
# - Tổng: 3,200k
# - Deposit (30%): 960k
```

### Test Case 2: Booking có ngày lễ Tết

```http
### 2. Tạo booking trong dịp Tết (28/01-03/02/2025)
POST https://localhost:7000/api/booking/create
Content-Type: application/json
Authorization: Bearer {{access_token}}

{
  "customerId": 1,
  "roomIds": [1, 3],  // Room 101, Room 201
  "checkInDate": "2025-01-28T14:00:00",
  "checkOutDate": "2025-01-30T12:00:00",
  "bookingType": "Online",
  "specialRequests": "Tet holiday booking"
}

# Kết quả mong đợi:
# - Room 101: (800k + 300k) × 2 đêm = 2,200k (có holiday pricing)
# - Room 201: (1,500k + 500k) × 2 đêm = 4,000k (có holiday pricing)
# - Tổng: 6,200k
# - Deposit (30%): 1,860k
```

### Test Case 3: Booking lễ 30/4 - 1/5

```http
### 3. Tạo booking trong dịp lễ 30/4
POST https://localhost:7000/api/booking/create
Content-Type: application/json
Authorization: Bearer {{access_token}}

{
  "customerId": 1,
  "roomIds": [1, 2],  // Room 101, 102
  "checkInDate": "2025-04-30T14:00:00",
  "checkOutDate": "2025-05-02T12:00:00",
  "bookingType": "Online"
}

# Kết quả mong đợi:
# - Room 101: (800k + 200k) × 2 đêm = 2,000k
# - Room 102: (800k + 200k) × 2 đêm = 2,000k
# - Tổng: 4,000k
# - Deposit (30%): 1,200k
```

## Helper Method để lấy CommonCode

```csharp
public class CommonCodeHelper
{
    private readonly IUnitOfWork _unitOfWork;

    public CommonCodeHelper(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy ID của CommonCode theo CodeType và CodeName
    /// </summary>
    public async Task<int?> GetCodeIdAsync(string codeType, string codeName)
    {
        var code = (await _unitOfWork.CommonCodes.FindAsync(c =>
            c.CodeType == codeType && c.CodeName == codeName)).FirstOrDefault();
        return code?.CodeId;
    }

    /// <summary>
    /// Lấy RoomStatus ID
    /// </summary>
    public async Task<int?> GetRoomStatusIdAsync(string statusName)
    {
        return await GetCodeIdAsync("RoomStatus", statusName);
    }

    /// <summary>
    /// Lấy PaymentStatus ID
    /// </summary>
    public async Task<int?> GetPaymentStatusIdAsync(string statusName)
    {
        return await GetCodeIdAsync("PaymentStatus", statusName);
    }

    /// <summary>
    /// Lấy BookingType ID
    /// </summary>
    public async Task<int?> GetBookingTypeIdAsync(string typeName)
    {
        return await GetCodeIdAsync("BookingType", typeName);
    }
}
```

## Ví dụ sử dụng trong Controller

```csharp
public class BookingController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        // ✅ Lấy BookingType từ CommonCode
        var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
            c.CodeType == "BookingType" && c.CodeName == request.BookingType)).FirstOrDefault();

        if (bookingTypeCode == null)
        {
            return BadRequest(new { message = $"Invalid booking type: {request.BookingType}" });
        }

        // ✅ Lấy Payment Status từ CommonCode
        var unpaidStatus = (await _unitOfWork.CommonCodes.FindAsync(c =>
            c.CodeType == "PaymentStatus" && c.CodeName == "Unpaid")).FirstOrDefault();

        var booking = new Booking
        {
            BookingTypeId = bookingTypeCode.CodeId,
            PaymentStatusId = unpaidStatus?.CodeId,
            // ... other fields
        };

        return Ok(booking);
    }
}
```

## Checklist khi code mới

- [ ] Kiểm tra xem field có phải là Status/Type không?
- [ ] Nếu có, đã lấy từ CommonCode chưa?
- [ ] Đã kiểm tra null cho kết quả trả về chưa?
- [ ] Đã sử dụng đúng CodeType và CodeName theo seeding data chưa?
- [ ] Đã test với data thực tế chưa?

## Lưu ý quan trọng

1. **KHÔNG BAO GIỜ** hardcode ID của CommonCode
2. **LUÔN LUÔN** sử dụng CodeName để tìm kiếm
3. **KIỂM TRA NULL** trước khi gán giá trị
4. Tham khảo file `SeedingData.cs` để biết chính xác CodeName
5. Nếu thêm CodeType mới, cập nhật document này

## Tài liệu tham khảo

- Seeding Data: `/AppBackend.ApiCore/Extensions/SeedingData.cs`
- Common Code Model: `/AppBackend.BusinessObjects/Models/CommonCode.cs`
- Common Code Repository: `/AppBackend.Repositories/Repositories/CommonCodeRepo/`

