# Giải thích: Gemini Function Calling & Date Validation

## 1. Tại sao Gemini gọi được function của bạn?

### **Function Calling (Tool Use)**
Gemini sử dụng tính năng **Function Calling** - cho phép AI model tự động gọi các function bạn định nghĩa khi cần.

**Cách hoạt động:**
1. Bạn định nghĩa function với `[KernelFunction]` và `[Description]`:
```csharp
[KernelFunction("search_available_rooms")]
[Description("Search for available hotel rooms...")]
public async Task<string> SearchAvailableRoomsAsync(
    [Description("Check-in date...")] string checkInDate,
    [Description("Check-out date...")] string checkOutDate
)
```

2. Semantic Kernel đăng ký function như "tool" cho Gemini:
```csharp
kernelBuilder.Plugins.AddFromObject(_bookingPlugin);
```

3. Khi user hỏi "Tôi muốn đặt phòng", Gemini AI:
   - Phân tích câu hỏi
   - Nhận ra cần gọi `search_available_rooms` function
   - Tự động gọi function với parameters phù hợp
   - Trả kết quả về cho user

4. `ToolCallBehavior = AutoInvokeKernelFunctions` cho phép tự động gọi:
```csharp
var executionSettings = new GeminiPromptExecutionSettings
{
    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
};
```

---

## 2. Calculate Salary theo tháng

**Vấn đề:** API `POST /api/SalaryInfo/calculate` với payload `{year: 2025}` trả 404.

**Giải pháp:** Cần thêm tham số `month` vào request. Hiện tại endpoint có thể chưa tồn tại hoặc cần sửa routing.

**Nên làm:**
```csharp
// Request model
public class CalculateSalaryRequest 
{
    public int Year { get; set; }
    public int Month { get; set; } // Thêm month
}

// API endpoint
[HttpPost("calculate")]
public async Task<IActionResult> CalculateSalary([FromBody] CalculateSalaryRequest request)
{
    var result = await _salaryService.CalculateSalaryAsync(request.Year, request.Month);
    return Ok(result);
}
```

---

## 3. Chatbot yêu cầu thông tin đầy đủ trước khi search phòng

**Đã sửa:** `HotelBookingPlugin.cs` - Thêm validation và description rõ ràng:

```csharp
[KernelFunction("search_available_rooms")]
[Description("IMPORTANT: You MUST ask user for check-in date and check-out date BEFORE calling this function.")]
public async Task<string> SearchAvailableRoomsAsync(
    [Description("Check-in date (REQUIRED - must ask user first)")] string checkInDate,
    [Description("Check-out date (REQUIRED - must ask user first)")] string checkOutDate
)
{
    // Validate required parameters
    if (string.IsNullOrWhiteSpace(checkInDate) || string.IsNullOrWhiteSpace(checkOutDate))
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            message = "Vui lòng cung cấp: Ngày check-in và ngày check-out",
            required_info = new[] { "check-in date", "check-out date" }
        });
    }
    // ...
}
```

**System prompt** trong `ChatHistoryService.cs` đã có hướng dẫn chi tiết:
- AI phải hỏi check-in và check-out date trước
- KHÔNG gọi function nếu thiếu thông tin
- Phải nhớ thông tin từ câu hỏi trước trong conversation

---

## 4. Object Cycle Error đã được xử lý

**Vấn đề:** JSON serialization gặp vòng lặp: `Comment -> Reply -> Comment -> Reply...`

**Đã sửa:** 
1. Sử dụng `ReferenceHandler.IgnoreCycles`:
```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    ReferenceHandler = ReferenceHandler.IgnoreCycles,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

2. Giảm data trả về - chỉ lấy thông tin cần thiết:
```csharp
var simplifiedRooms = roomList.Take(5).Select(r => new
{
    r.RoomTypeId,
    r.TypeName,
    BasePrice = r.BasePriceNight,
    // Chỉ lấy 3 amenities đầu
    Amenities = r.Amenities?.Take(3).Select(a => a.AmenityName).ToList()
}).ToList();
```

---

## 5. ✅ Check-In/Checkout chỉ cho phép đúng ngày

**Đã sửa:** Thêm validation ngày vào `BookingManagementService.cs` và `CheckoutService.cs`

### **Check-In Validation:**
```csharp
public async Task<ResultModel> CheckInBookingAsync(int bookingId, int employeeId)
{
    // Kiểm tra ngày check-in
    var today = DateTime.UtcNow.Date;
    var checkInDate = booking.CheckInDate.Date;
    
    if (today < checkInDate)
    {
        return new ResultModel
        {
            IsSuccess = false,
            StatusCode = StatusCodes.Status400BadRequest,
            Message = $"Chưa đến ngày check-in. Ngày check-in dự kiến: {checkInDate:dd/MM/yyyy}, Hôm nay: {today:dd/MM/yyyy}"
        };
    }
    // ... tiếp tục check-in
}
```

### **Checkout Validation:**
```csharp
public async Task<ResultModel> ProcessCheckoutAsync(CheckoutRequest request, int? processedBy = null)
{
    // Kiểm tra ngày checkout
    var today = DateTime.UtcNow.Date;
    var checkOutDate = booking.CheckOutDate.Date;
    
    if (today < checkOutDate)
    {
        return new ResultModel
        {
            IsSuccess = false,
            StatusCode = StatusCodes.Status400BadRequest,
            Message = $"Chưa đến ngày checkout. Ngày checkout dự kiến: {checkOutDate:dd/MM/yyyy}, Hôm nay: {today:dd/MM/yyyy}"
        };
    }
    // ... tiếp tục checkout
}
```

**Logic:**
- ✅ Check-in: Chỉ cho phép khi `hôm nay >= ngày check-in dự kiến`
- ✅ Checkout: Chỉ cho phép khi `hôm nay >= ngày checkout dự kiến`
- ❌ Nếu check-in/checkout sớm hơn → Trả lỗi 400 Bad Request

---

## 6. Xử lý 400 Bad Request từ Gemini

**Nguyên nhân phổ biến:**
1. Response quá lớn → Token limit exceeded
2. JSON format không hợp lệ
3. Circular reference trong data

**Đã khắc phục:**
- Giảm data trả về: chỉ lấy 5 rooms thay vì toàn bộ
- Truncate description nếu quá dài (>200 ký tự)
- Chỉ lấy 3 amenities thay vì toàn bộ
- Sử dụng `ReferenceHandler.IgnoreCycles`

---

## Testing

Sau khi sửa, test các trường hợp:

### **Test Chatbot:**
1. User: "Tôi muốn đặt phòng"
   - ✅ AI phải hỏi: "Anh/chị dự định check-in và checkout ngày nào?"
   - ❌ Không được gọi `search_available_rooms` ngay

2. User: "Tôi muốn đặt phòng ngày 15/01"
   - ✅ AI phải hỏi thêm: "Anh/chị dự định checkout ngày nào?"

3. User: "Check-in 15/01, checkout 17/01"
   - ✅ AI gọi `search_available_rooms(checkIn="2025-01-15", checkOut="2025-01-17")`
   - ✅ Trả về danh sách phòng

### **Test Check-In:**
```bash
# Booking check-in date: 2025-01-15
# Today: 2025-01-10
POST /api/BookingManagement/checkin/{bookingId}
# ❌ Response: "Chưa đến ngày check-in. Ngày check-in dự kiến: 15/01/2025, Hôm nay: 10/01/2025"

# Today: 2025-01-15 hoặc sau đó
POST /api/BookingManagement/checkin/{bookingId}
# ✅ Response: "Check-in thành công"
```

### **Test Checkout:**
```bash
# Booking checkout date: 2025-01-17
# Today: 2025-01-16
POST /api/Checkout/process
# ❌ Response: "Chưa đến ngày checkout. Ngày checkout dự kiến: 17/01/2025, Hôm nay: 16/01/2025"

# Today: 2025-01-17 hoặc sau đó
POST /api/Checkout/process
# ✅ Response: "Checkout thành công"
```

---

## Tóm tắt các thay đổi

| File | Thay đổi |
|------|----------|
| `HotelBookingPlugin.cs` | ✅ Validate required dates, giảm data response, fix object cycle |
| `BookingManagementService.cs` | ✅ Thêm date validation cho check-in |
| `CheckoutService.cs` | ✅ Thêm date validation cho checkout |
| `ChatHistoryService.cs` | ✅ Đã có system prompt hướng dẫn AI hỏi thông tin đầy đủ |

Tất cả đã được sửa và sẵn sàng để test! 🚀

