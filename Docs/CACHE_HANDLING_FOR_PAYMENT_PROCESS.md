# Cache Handling for Payment Process - Booking System

## Tổng quan

Document này mô tả chi tiết cách hệ thống xử lý cache để tránh tranh chấp (race condition) khi nhiều người đồng thời đặt phòng và trong quá trình thanh toán.

## Booking Type Configuration

### CommonCode Mapping

Khi tạo booking từ web, hệ thống sử dụng **BookingType = "Online"** để map với CommonCode:

```json
{
  "codeId": 33,
  "codeType": "BookingType",
  "codeValue": "Đặt trực tuyến",
  "codeName": "Online",
  "description": "Đặt phòng qua website/app",
  "isActive": true
}
```

### Cách sử dụng

1. **API Request từ Web:**
```json
{
  "customerId": 5,
  "roomTypes": [{"roomTypeId": 1, "quantity": 2}],
  "checkInDate": "2025-10-25T14:00:00Z",
  "checkOutDate": "2025-10-27T12:00:00Z",
  "bookingType": "Online"
}
```

2. **Nếu không gửi `bookingType`:**
   - Hệ thống tự động set = "Online" (default value)
   - Backend service tìm CommonCode với `codeName = "Online"`
   - Gán `booking.BookingTypeId = 33`

3. **Các BookingType có sẵn:**
   - `"Online"` → Đặt trực tuyến (web/app)
   - `"Walkin"` → Đặt tại quầy
   - `"Phone"` → Đặt qua điện thoại (nếu có)

### Backend Implementation

```csharp
// Service tự động tìm CommonCode
var bookingTypeCode = (await _unitOfWork.CommonCodes.FindAsync(c =>
    c.CodeType == "BookingType" && 
    c.CodeName == request.BookingType  // "Online"
)).FirstOrDefault();

if (bookingTypeCode != null) 
{
    booking.BookingTypeId = bookingTypeCode.CodeId; // 33
}
```

---

## Các loại Cache được sử dụng

### 1. ROOM BOOKING LOCK (Lock từng phòng cụ thể)

**Mục đích:** Ngăn chặn nhiều người cùng đặt 1 phòng trong cùng thời điểm

**Cache Key Format:**
```
room_booking_lock:{RoomId}_{CheckInDate:yyyyMMdd}_{CheckOutDate:yyyyMMdd}
```

**Ví dụ:**
```
room_booking_lock:101_20251025_20251027
```

**TTL (Time To Live):** 10 phút

**Lock Value:** GUID (LockId) - Đảm bảo chỉ người tạo lock mới release được

**Khi nào lock được giải phóng:**
- Thanh toán thành công
- Hủy booking
- Timeout (sau 10 phút tự động expire)

**Implementation:**
```csharp
var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
var lockId = Guid.NewGuid().ToString();
var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

// Release lock khi cần
_cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
```

---

### 2. BOOKING PAYMENT INFO (Thông tin booking chờ thanh toán)

**Mục đích:** Lưu thông tin tạm để xác thực thanh toán và tự động hủy nếu không thanh toán

**Cache Key Format:**
```
booking_payment:{BookingId}
```

**Ví dụ:**
```
booking_payment:123
```

**TTL:** 15 phút

**Data Structure:**
```json
{
  "BookingId": 123,
  "OrderCode": 251022143045,
  "Amount": 900000,
  "LockId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "RoomIds": [101, 102],
  "CheckInDate": "2025-10-25T14:00:00Z",
  "CheckOutDate": "2025-10-27T12:00:00Z",
  "CustomerEmail": "customer@example.com",
  "CustomerPhone": "0123456789"
}
```

**Implementation:**
```csharp
_cacheHelper.Set(CachePrefix.BookingPayment, booking.BookingId.ToString(), new
{
    BookingId = booking.BookingId,
    OrderCode = orderCode,
    Amount = booking.DepositAmount,
    LockId = lockId,
    RoomIds = lockedRoomIds,
    CheckInDate = request.CheckInDate,
    CheckOutDate = request.CheckOutDate
});

// Lấy info khi verify payment
var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, bookingId.ToString());
```

---

### 3. ROOM TYPE INVENTORY (Số lượng phòng theo loại)

**Mục đích:** Track số lượng phòng available theo loại để tránh overbooking khi nhiều request đồng thời

**Cache Key Format:**
```
room_type_inventory:{RoomTypeId}_{CheckInDate:yyyyMMdd}_{CheckOutDate:yyyyMMdd}
```

**Ví dụ:**
```
room_type_inventory:1_20251025_20251027
```

**TTL:** 15 phút

**Value:** Integer - Số lượng phòng available

**Operations:**
- `InitializeRoomTypeInventory`: Set giá trị ban đầu
- `DecrementRoomTypeInventory`: Giảm khi lock phòng
- `IncrementRoomTypeInventory`: Tăng khi release lock
- `GetRoomTypeInventory`: Lấy số lượng hiện tại

**Implementation:**
```csharp
// Khởi tạo inventory
_cacheHelper.InitializeRoomTypeInventory(roomTypeId, checkInDate, checkOutDate, availableCount);

// Giảm số lượng khi lock
var success = _cacheHelper.DecrementRoomTypeInventory(roomTypeId, checkInDate, checkOutDate, quantity);

// Tăng số lượng khi release
_cacheHelper.IncrementRoomTypeInventory(roomTypeId, checkInDate, checkOutDate, quantity);
```

---

## Các Scenario xử lý tranh chấp

### Scenario 1: Hai người cùng đặt phòng cùng loại cùng lúc

**Tình huống:**
- Có 3 phòng Standard còn trống
- User A yêu cầu 2 phòng Standard
- User B yêu cầu 2 phòng Standard (cùng lúc)

**Xử lý:**

1. **Request A và B cùng check availability**
   ```
   - Cả 2 đều thấy: "Còn 3 phòng Standard"
   ```

2. **Request A lock phòng trước**
   ```
   - Lock Room 101 → Success
   - Lock Room 102 → Success
   - Decrement inventory: 3 → 1
   ```

3. **Request B cố lock phòng**
   ```
   - Lock Room 101 → Fail (đã bị lock bởi A)
   - Tự động chọn Room 103 → Success
   - Lock Room 104 → Không tìm thấy phòng available
   - Decrement inventory fail (chỉ còn 1 phòng)
   ```

4. **Kết quả**
   ```
   - User A: Success, lock được 2 phòng
   - User B: Fail, không đủ phòng
   - User B nhận thông báo: "Chỉ còn 1 phòng trống, không đủ yêu cầu 2 phòng"
   ```

---

### Scenario 2: Timeout thanh toán

**Tình huống:**
- User tạo booking nhưng không thanh toán trong 15 phút

**Xử lý:**

1. **Tạo booking (T=0)**
   ```
   - Lock 2 phòng trong cache (10 phút)
   - Tạo booking record trong DB
   - Lưu payment info vào cache (15 phút)
   - Schedule auto-cancel task
   ```

2. **Sau 10 phút (T=10)**
   ```
   - Room locks tự động expire
   - Phòng có thể được người khác lock
   ```

3. **Sau 15 phút (T=15)**
   ```
   - Check payment info trong cache
   - Nếu vẫn còn → Payment chưa complete
   - Trigger auto-cancel:
     * Update booking status = Cancelled
     * Remove payment info khỏi cache
     * Increment inventory
   ```

**NOTE:** Lock expire sau 10 phút nhưng auto-cancel sau 15 phút để đảm bảo user có thời gian thanh toán ngay cả khi lock hết hạn.

---

### Scenario 3: Thanh toán thành công

**Tình huống:**
- User thanh toán thành công qua PayOS

**Xử lý:**

1. **PayOS gọi webhook confirm-payment**
   ```
   POST /api/booking/confirm-payment
   {
     "bookingId": 123,
     "orderCode": "251022143045",
     "status": "PAID"
   }
   ```

2. **Backend verify payment**
   ```
   - Lấy payment info từ cache
   - Verify với PayOS API
   - Check status = "PAID"
   ```

3. **Update database**
   ```
   - Update booking.DepositStatusId = Paid
   - Update transaction.TransactionStatusId = Success
   ```

4. **Release cache**
   ```
   - ReleaseAllBookingLocks(roomIds, checkInDate, checkOutDate, lockId)
   - Remove payment info: Remove(BookingPayment, bookingId)
   ```

5. **Kết quả**
   ```
   - Phòng chính thức được booked
   - Locks được giải phóng
   - Inventory không cần increment (phòng đã booked thật)
   ```

---

### Scenario 4: Hủy booking

**Tình huống:**
- User hoặc hệ thống hủy booking

**Xử lý:**

1. **Lấy thông tin booking từ DB**
   ```
   - BookingId, RoomIds, CheckInDate, CheckOutDate
   - LockId từ payment cache (nếu còn)
   ```

2. **Release locks**
   ```csharp
   foreach (var roomId in roomIds)
   {
       var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
       _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
   }
   ```

3. **Increment inventory**
   ```csharp
   // Trả lại số lượng phòng theo từng loại
   var roomTypeGroups = rooms.GroupBy(r => r.RoomTypeId);
   foreach (var group in roomTypeGroups)
   {
       _cacheHelper.IncrementRoomTypeInventory(
           group.Key, 
           checkInDate, 
           checkOutDate, 
           group.Count()
       );
   }
   ```

4. **Update database**
   ```
   - Update booking status = Cancelled
   - Remove payment info khỏi cache
   ```

---

## Best Practices

### 1. Luôn check cache trước database

```csharp
// BAD
var existingBookings = await _unitOfWork.BookingRooms.FindAsync(...);

// GOOD
var lockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);
if (!string.IsNullOrEmpty(lockedBy))
{
    // Phòng đã bị lock, skip
    continue;
}

// Sau đó mới check database
var existingBookings = await _unitOfWork.BookingRooms.FindAsync(...);
```

### 2. Release lock ngay khi không cần

```csharp
// Nếu có lỗi, release tất cả locks đã tạo
foreach (var lockedRoomId in lockedRoomIds)
{
    var releaseLockKey = $"{lockedRoomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
    _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
}
```

### 3. Sử dụng LockId (GUID) để đảm bảo an toàn

```csharp
var lockId = Guid.NewGuid().ToString();
var locked = _cacheHelper.TryAcquireLock(CachePrefix.RoomBookingLock, lockKey, lockId);

// Chỉ người có lockId mới release được
_cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, lockKey, lockId);
```

### 4. Handle rollback khi tạo payment link fail

```csharp
try
{
    var createPayment = await _payOS.createPaymentLink(paymentData);
}
catch (Exception ex)
{
    // Rollback: Delete booking
    await _unitOfWork.Bookings.DeleteAsync(booking);
    
    // Release locks
    foreach (var roomId in lockedRoomIds)
    {
        var releaseLockKey = $"{roomId}_{checkInDate:yyyyMMdd}_{checkOutDate:yyyyMMdd}";
        _cacheHelper.ReleaseLock(CachePrefix.RoomBookingLock, releaseLockKey, lockId);
    }
    
    return error;
}
```

### 5. Monitor cache performance

```csharp
// Log cache operations để tracking
_logger.LogInformation($"Lock acquired for Room {roomId}: {lockId}");
_logger.LogInformation($"Inventory decremented: RoomType {roomTypeId}, New count: {newCount}");
```

---

## API Documentation

Xem chi tiết tại `BookingController.cs` - Đã được thêm comprehensive documentation cho:

- `POST /api/booking/check-availability` - Check phòng available với cache
- `POST /api/booking` - Tạo booking với lock mechanism
- `POST /api/booking/guest` - Guest booking với cache tracking
- `POST /api/booking/confirm-payment` - Confirm payment và release locks
- `DELETE /api/booking/{id}` - Cancel booking và release cache

---

## Testing Cache Handling

### Test Case 1: Concurrent booking requests

```bash
# Terminal 1
curl -X POST http://localhost:5000/api/booking \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "roomTypes": [{"roomTypeId": 1, "quantity": 2}],
    "checkInDate": "2025-10-25T14:00:00Z",
    "checkOutDate": "2025-10-27T12:00:00Z"
  }'

# Terminal 2 (cùng lúc)
curl -X POST http://localhost:5000/api/booking \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 2,
    "roomTypes": [{"roomTypeId": 1, "quantity": 2}],
    "checkInDate": "2025-10-25T14:00:00Z",
    "checkOutDate": "2025-10-27T12:00:00Z"
  }'
```

**Expected:** Một request thành công, request còn lại báo không đủ phòng

### Test Case 2: Payment timeout

1. Tạo booking
2. Đợi 15 phút
3. Check booking status → Should be Cancelled
4. Check phòng availability → Should be available again

---

## Monitoring và Troubleshooting

### Kiểm tra cache state

```csharp
// Check room lock
var lockKey = $"101_20251025_20251027";
var lockedBy = _cacheHelper.Get<string>(CachePrefix.RoomBookingLock, lockKey);
Console.WriteLine($"Room locked by: {lockedBy}");

// Check inventory
var inventory = _cacheHelper.GetRoomTypeInventory(1, checkInDate, checkOutDate);
Console.WriteLine($"Available rooms: {inventory}");

// Check payment info
var paymentInfo = _cacheHelper.Get<dynamic>(CachePrefix.BookingPayment, "123");
Console.WriteLine($"Payment info: {JsonSerializer.Serialize(paymentInfo)}");
```

### Common Issues

1. **Lock không được release**
   - Check TTL đã expire chưa
   - Verify LockId có khớp không
   - Check logs để xem có exception trong quá trình release không

2. **Inventory không đúng**
   - Cache có thể bị clear do restart server
   - Implement logic rebuild cache từ database khi cần

3. **Race condition vẫn xảy ra**
   - Check xem có dùng distributed cache (Redis) thay vì in-memory không
   - Verify atomic operations trong cache

---

## Future Improvements

1. **Migrate to Redis** cho distributed caching
2. **Implement cache warming** khi server start
3. **Add cache metrics** để monitor hit rate
4. **Implement circuit breaker** cho cache operations
5. **Add retry logic** cho failed cache operations

---

**Last Updated:** 2025-10-22
**Maintained by:** Backend Team
