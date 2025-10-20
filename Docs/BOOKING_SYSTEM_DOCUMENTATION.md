# Booking System với Message Queue và Cache Locking

## Tổng quan

Hệ thống đặt phòng sử dụng:
- **MemoryCache** để lock phòng tránh race condition
- **Message Queue (Channel)** để xử lý bất đồng bộ
- **PayOS** để thanh toán online

## Luồng hoạt động

### 1. Tạo Booking

```
User Request → Check Availability → Lock Rooms in Cache (10 phút)
→ Create Booking in DB → Generate PayOS Payment Link
→ Return BookingId + Payment URL
→ Enqueue message to Queue → Auto-cancel after 15 phút nếu chưa thanh toán
```

### 2. Thanh toán

```
User pays via PayOS → PayOS callback → Confirm Payment API
→ Update Booking Status → Release Room Locks → Remove from Cache
```

### 3. Hủy Booking

```
User Cancel → Enqueue Cancel Message → Queue Processor
→ Release Room Locks → Delete Booking → Clear Cache
```

## Các thành phần

### 1. CacheHelper
- **RoomBookingLock**: Lock phòng trong 10 phút
- **BookingPayment**: Lưu thông tin thanh toán trong 15 phút

### 2. Message Queue
- **BookingQueueService**: Channel-based queue (max 1000 messages)
- **BookingQueueProcessor**: Background service xử lý queue 24/7
- **Message Types**:
  - CreateBooking
  - ConfirmPayment
  - CancelBooking
  - ReleaseRoomLock

### 3. BookingService
**Chức năng:**
- CheckRoomAvailability: Kiểm tra phòng trống
- CreateBooking: Tạo booking với room locking
- ConfirmPayment: Xác nhận thanh toán từ PayOS
- CancelBooking: Hủy booking và release locks

### 4. Background Service
**BookingQueueProcessor** chạy liên tục để:
- Xử lý messages từ queue
- Release locks khi timeout
- Cancel booking chưa thanh toán sau 15 phút
- Retry logic (max 3 lần)

## API Endpoints

### 1. POST /api/Booking/check-availability
Kiểm tra phòng có sẵn không

**Request:**
```json
{
  "roomIds": [1, 2],
  "checkInDate": "2025-10-20T14:00:00",
  "checkOutDate": "2025-10-22T12:00:00"
}
```

**Response (Success):**
```json
{
  "isSuccess": true,
  "message": "Tất cả phòng đều khả dụng",
  "statusCode": 200
}
```

**Response (Conflict):**
```json
{
  "isSuccess": false,
  "message": "Một số phòng không khả dụng trong khoảng thời gian này",
  "data": [
    {
      "roomId": 1,
      "roomNumber": "101",
      "checkInDate": "2025-10-20T14:00:00",
      "checkOutDate": "2025-10-22T12:00:00",
      "lockedBy": "uuid-lock-id",
      "lockExpiry": "2025-10-20T14:10:00"
    }
  ],
  "statusCode": 409
}
```

### 2. POST /api/Booking
Tạo booking mới (yêu cầu authentication)

**Request:**
```json
{
  "customerId": 1,
  "roomIds": [1, 2],
  "checkInDate": "2025-10-20T14:00:00",
  "checkOutDate": "2025-10-22T12:00:00",
  "specialRequests": "Tầng cao, view biển",
  "bookingType": "Online"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Tạo booking thành công. Vui lòng thanh toán trong 15 phút!",
  "data": {
    "bookingId": 123,
    "customerId": 1,
    "customerName": "Nguyễn Văn A",
    "roomIds": [1, 2],
    "checkInDate": "2025-10-20T14:00:00",
    "checkOutDate": "2025-10-22T12:00:00",
    "totalAmount": 3200000,
    "depositAmount": 960000,
    "paymentUrl": "https://pay.payos.vn/web/abc123",
    "createdAt": "2025-10-18T10:30:00"
  },
  "statusCode": 201
}
```

### 3. GET /api/Booking/{bookingId}
Lấy thông tin booking

### 4. POST /api/Booking/confirm-payment
Xác nhận thanh toán (webhook từ PayOS)

**Request:**
```json
{
  "bookingId": 123,
  "orderCode": "251018103000",
  "status": "PAID"
}
```

### 5. GET /api/Booking/my-bookings
Lấy danh sách booking của khách hàng

### 6. DELETE /api/Booking/{bookingId}
Hủy booking

## Cơ chế phòng Race Condition

### Scenario: 2 người đặt cùng 1 phòng cuối cùng

**User A** và **User B** cùng đặt Room 101:

1. **User A**: Gọi API CreateBooking
   - Lock Room 101 với LockId_A → Success
   - Create Booking A
   - Return Payment URL

2. **User B**: Gọi API CreateBooking (1 giây sau)
   - Try Lock Room 101 → **Failed** (đã bị A lock)
   - Release all locks đã thử
   - Return 409 Conflict: "Phòng đang được đặt bởi người khác"

3. **User A**: Thanh toán thành công
   - Confirm Payment → Release Lock
   - Room 101 available lại

4. **User B**: Thử lại
   - Lock Room 101 → Success
   - Create Booking B

### Timeout Protection

**Trường hợp User A không thanh toán:**
- Lock tự động expire sau 10 phút
- Booking tự động cancel sau 15 phút
- Room 101 available cho người khác

## Configuration

### appsettings.json

```json
{
  "PayOS": {
    "ClientId": "your-client-id",
    "ApiKey": "your-api-key",
    "ChecksumKey": "your-checksum-key",
    "ReturnUrl": "http://localhost:5173/payment/callback",
    "CancelUrl": "http://localhost:5173/payment/cancel"
  }
}
```

## Testing

1. **Test Race Condition:**
   - Mở 2 tab browser
   - Đặt cùng 1 phòng cùng lúc
   - Kiểm tra chỉ 1 người được đặt thành công

2. **Test Timeout:**
   - Tạo booking
   - Không thanh toán
   - Chờ 15 phút → Booking tự động cancel

3. **Test Payment:**
   - Tạo booking
   - Click PayOS payment link
   - Thanh toán thành công
   - Verify booking status updated

## Monitoring

**Queue Status:**
```csharp
var queueService = serviceProvider.GetService<IBookingQueueService>();
var queueCount = queueService.GetQueueCount();
Console.WriteLine($"Messages in queue: {queueCount}");
```

**Cache Status:**
```csharp
var cacheHelper = serviceProvider.GetService<CacheHelper>();
var lockInfo = cacheHelper.Get<string>(CachePrefix.RoomBookingLock, "1_20251020_20251022");
Console.WriteLine($"Room lock: {lockInfo}");
```

## Troubleshooting

### Problem: Phòng bị lock nhưng không có booking
**Solution:** Lock tự động expire sau 10 phút

### Problem: Message queue đầy
**Solution:** Tăng BoundedChannelOptions capacity trong BookingQueueService

### Problem: Background service không chạy
**Solution:** Check logs, verify AddHostedService đã được đăng ký trong Program.cs

## Performance

- **Lock Operations**: O(1) - MemoryCache lookup
- **Queue Operations**: O(1) - Channel write/read
- **Database**: Async operations với EF Core
- **Concurrency**: Thread-safe với Channel và MemoryCache

## Security

- Authentication required cho CreateBooking
- BookingId validation
- Payment verification với PayOS
- Lock ownership verification (LockId)

