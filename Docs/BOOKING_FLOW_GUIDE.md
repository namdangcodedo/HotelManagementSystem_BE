# Hướng Dẫn Luồng Booking Chi Tiết

## 📋 Tổng Quan

Hệ thống booking khách sạn sử dụng kiến trúc bất đồng bộ với:
- **MemoryCache**: Lock phòng tránh race condition
- **Message Queue (Channel)**: Xử lý background tasks
- **PayOS Integration**: Thanh toán online
- **Background Service**: Auto-cancel bookings chưa thanh toán

---

## 🔄 Luồng Chính

### 1️⃣ **KIỂM TRA PHÒNG TRỐNG**

```
User → POST /api/Booking/check-availability
       ↓
   Check Cache (Room Locks)
       ↓
   Check Database (Existing Bookings)
       ↓
   ├─→ All Available → Return 200 OK
   └─→ Some Locked/Booked → Return 409 Conflict
```

**API Request:**
```json
POST /api/Booking/check-availability
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
  "message": "Một số phòng không khả dụng",
  "data": [
    {
      "roomId": 1,
      "roomNumber": "101",
      "lockedBy": "uuid-abc-123",
      "lockExpiry": "2025-10-20T14:10:00"
    }
  ],
  "statusCode": 409
}
```

---

### 2️⃣ **TẠO BOOKING**

```
User → POST /api/Booking/create (với JWT token)
       ↓
   [1] Validate Request
       ↓
   [2] Check Authentication
       ↓
   [3] Lock Rooms in Cache (10 phút)
       ↓ (nếu lock thất bại)
       ├─→ Return 409 "Phòng đang được đặt"
       ↓ (nếu lock thành công)
   [4] Calculate Total Amount (với Holiday Pricing nếu có)
       ↓
   [5] Create Booking in Database (Status: Pending)
       ↓
   [6] Generate PayOS Payment Link
       ↓
   [7] Return BookingId + Payment URL
       ↓
   [8] Enqueue "Auto-Cancel" Message (15 phút)
```

**API Request:**
```json
POST /api/Booking/create
Authorization: Bearer {token}
{
  "customerId": 1,
  "roomIds": [1, 2],
  "checkInDate": "2025-10-20T14:00:00",
  "checkOutDate": "2025-10-22T12:00:00",
  "bookingType": "Online",
  "specialRequests": "Tầng cao, view biển"
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
    "createdAt": "2025-10-20T10:30:00",
    "paymentDeadline": "2025-10-20T10:45:00"
  },
  "statusCode": 201
}
```

---

### 3️⃣ **THANH TOÁN**

#### Scenario A: Thanh toán thành công (trong 15 phút)

```
User → Click PayOS Payment URL
       ↓
   PayOS Payment Gateway
       ↓
   User nhập thông tin thanh toán
       ↓
   PayOS → POST /api/Booking/confirm-payment (webhook)
       ↓
   [1] Validate orderCode & bookingId
       ↓
   [2] Update Booking Status → "Paid"
       ↓
   [3] Update Payment Status → "Paid"
       ↓
   [4] Release Room Locks from Cache
       ↓
   [5] Remove Auto-Cancel Message from Queue
       ↓
   [6] Send Confirmation Email (optional)
       ↓
   Return Success → PayOS → User
```

**Confirm Payment Request:**
```json
POST /api/Booking/confirm-payment
{
  "bookingId": 123,
  "orderCode": "251020103000",
  "status": "PAID"
}
```

#### Scenario B: Không thanh toán (sau 15 phút)

```
Background Service → Check Queue every 1 minute
       ↓
   Found "Auto-Cancel" message (15 phút đã qua)
       ↓
   [1] Check Booking Status
       ↓ (nếu vẫn Pending)
   [2] Update Booking Status → "Cancelled"
       ↓
   [3] Release Room Locks from Cache
       ↓
   [4] Log Cancel Reason: "Payment Timeout"
       ↓
   [5] Send Cancellation Email (optional)
```

---

### 4️⃣ **HỦY BOOKING (Manual)**

```
User → POST /api/Booking/cancel/{bookingId}
       ↓
   [1] Validate Authentication
       ↓
   [2] Check Ownership (User phải là chủ booking hoặc Admin)
       ↓ (nếu không phải)
       ├─→ Return 403 Forbidden
       ↓ (nếu được phép)
   [3] Check Booking Status
       ↓ (nếu đã Paid)
       ├─→ Require refund process (not auto-cancel)
       ↓ (nếu Pending)
   [4] Update Booking Status → "Cancelled"
       ↓
   [5] Release Room Locks from Cache
       ↓
   [6] Remove from Payment Queue
       ↓
   [7] Return Success
```

**API Request:**
```http
POST /api/Booking/cancel/123
Authorization: Bearer {token}
```

---

## 🔒 Cơ Chế Phòng Race Condition

### Scenario: 2 người đặt cùng 1 phòng

```
TIME: 10:00:00
User A → POST /api/Booking/create (Room 101)
         Lock Room 101 with LockId_A ✅
         Create Booking A
         Return Payment URL

TIME: 10:00:01 (1 giây sau)
User B → POST /api/Booking/create (Room 101)
         Try Lock Room 101 ❌ (Đã bị User A lock)
         Return 409 Conflict: "Phòng đang được đặt bởi người khác"

TIME: 10:02:00 (2 phút sau)
User A → Pay via PayOS ✅
         Confirm Payment
         Release Lock Room 101
         Room 101 available lại

TIME: 10:03:00
User B → Retry POST /api/Booking/create (Room 101)
         Lock Room 101 with LockId_B ✅
         Create Booking B
         Return Payment URL
```

### Cache Lock Structure:

```
Key: "RoomBookingLock:1_20251020_20251022"
Value: {
  "lockId": "uuid-abc-123",
  "bookingId": 123,
  "lockedBy": 1,
  "lockedAt": "2025-10-20T10:00:00",
  "expiresAt": "2025-10-20T10:10:00"
}
TTL: 10 minutes
```

---

## ⏱️ Timing & Timeouts

| Event | Timeout | Action |
|-------|---------|--------|
| **Room Lock** | 10 phút | Auto-release nếu không confirm |
| **Payment Deadline** | 15 phút | Auto-cancel booking |
| **Queue Retry** | 3 lần | Exponential backoff: 1s, 2s, 4s |
| **Background Check** | Every 1 phút | Scan expired bookings |

---

## 💰 Holiday Pricing Integration

Khi tạo booking, hệ thống tự động:

1. **Check ngày check-in & check-out**
2. **Query Holiday table** để tìm các ngày lễ trong khoảng thời gian
3. **Calculate giá:**
   ```
   Đêm thường: BasePriceNight
   Đêm lễ: BasePriceNight + HolidayPriceAdjustment
   ```
4. **Ví dụ:**
   - Room 101: 800k/đêm (thường)
   - Tết: +300k → 1,100k/đêm
   - Booking 2 đêm Tết: 2 × 1,100k = 2,200k

---

## 🔍 Kiểm Tra Trạng Thái

### 1. Xem Booking Details
```http
GET /api/Booking/{bookingId}
Authorization: Bearer {token}
```

### 2. Xem Danh Sách Booking của mình
```http
GET /api/Booking/my-bookings
Authorization: Bearer {token}
```

### 3. Xem Transaction History
```http
GET /api/Transaction/booking/{bookingId}
Authorization: Bearer {token}
```

---

## 📊 Booking Status Flow

```
┌─────────┐
│ Pending │ ← Booking vừa tạo
└────┬────┘
     │
     ├──→ [Thanh toán] ──→ ┌──────┐
     │                      │ Paid │ ← Đã thanh toán
     │                      └──────┘
     │
     ├──→ [Hủy manual] ──→ ┌───────────┐
     │                      │ Cancelled │
     └──→ [Timeout 15p] ──→ └───────────┘
```

---

## 🧪 Testing Flow

### Test Case 1: Happy Path (Đặt phòng thành công)
1. Đăng nhập → Lấy token
2. Check availability → 200 OK
3. Create booking → 201 Created (có payment URL)
4. Click payment URL → Thanh toán
5. PayOS callback → confirm-payment
6. Get booking details → Status = "Paid"

### Test Case 2: Race Condition
1. Mở 2 browser tab
2. Cùng lúc đặt Room 101
3. Tab 1: Success ✅
4. Tab 2: 409 Conflict ❌

### Test Case 3: Payment Timeout
1. Create booking
2. KHÔNG thanh toán
3. Chờ 15 phút
4. Get booking details → Status = "Cancelled"

### Test Case 4: Cancel Before Payment
1. Create booking
2. Cancel ngay lập tức
3. Status = "Cancelled"
4. Room available lại

---

## 🚨 Error Codes

| Code | Message | Meaning |
|------|---------|---------|
| **200** | OK | Request thành công |
| **201** | Created | Booking tạo thành công |
| **400** | Bad Request | Dữ liệu không hợp lệ |
| **401** | Unauthorized | Chưa đăng nhập |
| **403** | Forbidden | Không có quyền |
| **404** | Not Found | Booking không tồn tại |
| **409** | Conflict | Phòng đã được đặt/lock |
| **500** | Server Error | Lỗi hệ thống |

---

## 🔧 Troubleshooting

### Problem 1: Phòng bị lock mãi
**Nguyên nhân:** Cache lock chưa expire  
**Giải pháp:** Chờ 10 phút hoặc restart server (dev only)

### Problem 2: Booking không tự động cancel
**Nguyên nhân:** Background service không chạy  
**Giải pháp:** Check logs, verify `BookingQueueProcessor` đã được register

### Problem 3: PayOS callback không về
**Nguyên nhân:** URL không public hoặc firewall block  
**Giải pháp:** Use ngrok để expose localhost

### Problem 4: 2 booking cùng phòng cùng thời gian
**Nguyên nhân:** Cache lock không hoạt động  
**Giải pháp:** Check `MemoryCache` configuration trong `Program.cs`

---

## 📝 Notes

✅ **Authentication required** cho tất cả API trừ:
- `check-availability` (public)
- `confirm-payment` (webhook từ PayOS)

✅ **Authorization rules:**
- User chỉ xem/cancel booking của mình
- Admin xem/cancel bất kỳ booking nào

✅ **Deposit Amount:**
- Mặc định: 30% tổng tiền
- Có thể config trong database

✅ **Payment Methods:**
- Hiện tại: PayOS only
- Tương lai: VNPay, MoMo, Cash (offline)

---

## 🎯 Quick Reference

**Tạo booking mới:**
```bash
POST /api/Booking/create
Auth: Required
Response: bookingId + paymentUrl
```

**Check phòng trống:**
```bash
POST /api/Booking/check-availability
Auth: Not required
Response: Available/Conflict
```

**Hủy booking:**
```bash
POST /api/Booking/cancel/{id}
Auth: Required
Response: Success/Forbidden
```

**Xem booking của mình:**
```bash
GET /api/Booking/my-bookings
Auth: Required
Response: List of bookings
```

---

## 📚 Related Documentation

- [API Testing Guide](./API_TESTS.md)
- [Booking Configuration](./BOOKING_CONFIGURATION_SUMMARY.md)
- [Holiday Pricing](./test-booking-holiday-pricing.http)
- [Architecture Overview](./PROJECT_ARCHITECTURE.md)

