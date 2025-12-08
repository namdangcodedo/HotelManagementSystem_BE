# Booking API Flow v2 (Check Availability → Booking → QR Payment/Cancel)

This document describes the current booking flow implemented in the backend after recent changes.

Main steps:

1. Check room-type availability: `POST /api/Booking/check-availability`
2. Create booking for logged-in user: `POST /api/Booking`
3. Create booking for guest (no account): `POST /api/Booking/guest`
4. Get booking details by ID (authorized): `GET /api/Booking/{bookingId}`
5. Get booking details by token (guest view): `GET /api/Booking/mybooking/{token}`
6. Notify payment or cancel booking: `POST /api/Booking/confirm-payment`
7. Cancel booking (shortcut for logged-in user): `DELETE /api/Booking/{bookingId}`
8. Get all bookings of current account: `GET /api/Booking/my-bookings`

All APIs return a common wrapper `ResultModel`:

```jsonc
{
  "isSuccess": true,
  "message": "...",
  "statusCode": 200,
  "data": { /* payload depends on endpoint */ }
}
```

---

## 1. Check room-type availability

**Endpoint**  
`POST /api/Booking/check-availability`  
Auth: **AllowAnonymous**

### Request body

Model: `CheckRoomAvailabilityRequest`

```jsonc
{
  "roomTypes": [
    { "roomTypeId": 1, "quantity": 2 },
    { "roomTypeId": 3, "quantity": 1 }
  ],
  "checkInDate": "2025-12-20T14:00:00Z",
  "checkOutDate": "2025-12-22T12:00:00Z"
}
```

- `roomTypes` (array of `RoomTypeQuantityRequest`)
  - `roomTypeId` (int) – ID of the room type.
  - `quantity` (int) – number of rooms requested for that type.
- `checkInDate` (DateTime) – start time of stay.
- `checkOutDate` (DateTime) – end time of stay.

### Successful response (200 OK or 409 Conflict)

`BookingService.CheckRoomAvailabilityAsync` returns a `CheckAvailabilityResponse` in `data`:

```jsonc
{
  "isSuccess": true,
  "statusCode": 200, // or 409 if some room types are partially available
  "message": "Tất cả phòng đều có sẵn",
  "data": {
    "isAllAvailable": true,
    "message": "Tất cả phòng đều có sẵn",
    "roomTypes": [
      {
        "roomTypeId": 1,
        "roomTypeName": "Standard",
        "roomTypeCode": "STD",
        "description": "Phòng tiêu chuẩn",
        "basePriceNight": 500000,
        "maxOccupancy": 2,
        "roomSize": 20,
        "numberOfBeds": 1,
        "bedType": "Queen",
        "availableCount": 5,
        "requestedQuantity": 2,
        "isAvailable": true,
        "message": "Có 5 phòng trống",
        "images": []
      },
      {
        "roomTypeId": 3,
        "roomTypeName": "Suite",
        "roomTypeCode": "STE",
        "description": "Phòng suite",
        "basePriceNight": 1500000,
        "maxOccupancy": 4,
        "roomSize": 40,
        "numberOfBeds": 2,
        "bedType": "King",
        "availableCount": 0,
        "requestedQuantity": 1,
        "isAvailable": false,
        "message": "Chỉ còn 0/1 phòng trống",
        "images": []
      }
    ],
    "checkInDate": "2025-12-20T14:00:00Z",
    "checkOutDate": "2025-12-22T12:00:00Z",
    "totalNights": 2
  }
}
```

- `statusCode`:
  - `200` if `isAllAvailable = true`.
  - `409` if **any** requested room type is not fully available.

---

## 2. Create booking for logged-in user

**Endpoint**  
`POST /api/Booking`  
Auth: **[Authorize]** (JWT – `CurrentUserId` used as `userId`)

### Request body

Model: `CreateBookingRequest`

```jsonc
{
  "roomTypes": [
    { "roomTypeId": 1, "quantity": 2 },
    { "roomTypeId": 3, "quantity": 1 }
  ],
  "checkInDate": "2025-12-20T14:00:00Z",
  "checkOutDate": "2025-12-22T12:00:00Z",
  "specialRequests": "Tầng cao, view đẹp"
}
```

Validation rules (from `CreateBookingAsync`):

- `checkInDate` must be **on or after today** (compared to `DateTime.UtcNow.Date`); if earlier → `400 BadRequest`.
- `checkOutDate` must be **after** `checkInDate`; if not → `400 BadRequest`.
- The current account (`CurrentUserId`) must exist in `Account` table; otherwise `404`.
- A `Customer` linked to that account must exist; otherwise `404`.
- For each `roomType`:
  - Room type must exist; otherwise `404`.
  - Enough available rooms must be found for the requested date range; otherwise `409 Conflict`.

### Business logic

1. Validate dates and user/customer.
2. For each requested room type:
   - Find available rooms via `BookingHelperService.FindAvailableRoomsByTypeAsync` (re-check availability on server).
   - Calculate price per room with holiday pricing via `BookingHelperService.CalculateRoomPriceWithHolidayAsync`.
3. Sum up `totalAmount` for the booking.
4. Compute `depositAmount = totalAmount * 0.3`.
5. Resolve `BookingStatus = Pending` and `BookingType = Online` from `CommonCode`.
6. Create `Booking` entity with status `Pending` and type `Online`.
7. Create `BookingRoom` rows for each selected room with `pricePerNight`, `numberOfNights`, and `subTotal`.
8. Generate a transaction reference and **QR payment info** via `QRPaymentHelper.GenerateQRPaymentInfoAsync`.
9. Store payment tracking data in cache `BookingPayment:{bookingId}` with TTL 15 minutes:
   - `bookingId`, `transactionRef`, `roomIds`, `checkInDate`, `checkOutDate`, `amount` (deposit).
10. Schedule a timeout check via `BookingTimeoutChecker.ScheduleTimeoutCheck(bookingId, 15)`.
11. Generate a booking token via `BookingTokenHelper.EncodeBookingId(bookingId)`.
12. Build `BookingDto` and return it with QR payment info.

### Successful response (201 Created)

`data` contains an object with booking info, token and QR payment:

```jsonc
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Tạo booking thành công. Vui lòng quét mã QR để thanh toán trong 15 phút!",
  "data": {
    "booking": {
      "bookingId": 123,
      "customerId": 10,
      "customerName": "Nguyễn Văn A",
      "customerEmail": "",          // currently not populated in service
      "customerPhone": "",          // currently not populated in service
      "roomIds": [101, 102, 203],
      "roomNames": ["101", "102", "203"],
      "roomTypeDetails": [
        {
          "roomTypeId": 1,
          "roomTypeName": "Standard",
          "roomTypeCode": "STD",
          "quantity": 2,
          "pricePerNight": 500000,
          "subTotal": 2000000
        },
        {
          "roomTypeId": 3,
          "roomTypeName": "Suite",
          "roomTypeCode": "STE",
          "quantity": 1,
          "pricePerNight": 1500000,
          "subTotal": 1500000
        }
      ],
      "checkInDate": "2025-12-20T14:00:00Z",
      "checkOutDate": "2025-12-22T12:00:00Z",
      "totalAmount": 3500000,
      "depositAmount": 1050000,
      "paymentStatus": "Pending",
      "depositStatus": "",           // currently empty in service
      "bookingType": "Online",
      "specialRequests": "Tầng cao, view đẹp",
      "createdAt": "2025-12-09T10:00:00Z",
      "paymentUrl": null,             // QR info returned separately
      "orderCode": "TRX-202512091000-123" // generated transaction ref
    },
    "bookingToken": "{token-string}",
    "qrPayment": {
      // structure defined by QRPaymentHelper.GenerateQRPaymentInfoAsync
      // typically includes: bank info, account number, amount, content, 
      // and an encoded QR image/data string
    },
    "paymentDeadline": "2025-12-09T10:15:00Z"
  }
}
```

### Possible error responses

- `400 BadRequest`
  - `"Ngày check-in phải sau thời điểm hiện tại"`
  - `"Ngày check-out phải sau ngày check-in"`
- `404 NotFound`
  - `"Không tìm thấy tài khoản"`
  - `"Không tìm thấy thông tin khách hàng"`
  - `"Không tìm thấy loại phòng ID: {roomTypeId}"`
- `409 Conflict`
  - `"Không đủ phòng {roomTypeName}. Còn {available}/{requested}"`
- `500 InternalServerError`
  - `"Lỗi cấu hình hệ thống: Thiếu status codes"`
  - `"Lỗi: {exceptionMessage}"`

---

## 3. Create guest booking (no account required)

**Endpoint**  
`POST /api/Booking/guest`  
Auth: **AllowAnonymous**

### Request body

Model: `CreateGuestBookingRequest`

```jsonc
{
  "fullName": "Nguyễn Văn B",
  "email": "guest@example.com",
  "phoneNumber": "0912345678",
  "identityCard": "012345678901",
  "address": "Hà Nội",
  "roomTypes": [
    { "roomTypeId": 1, "quantity": 1 }
  ],
  "checkInDate": "2025-12-20T14:00:00Z",
  "checkOutDate": "2025-12-22T12:00:00Z",
  "specialRequests": "Check-in sớm"
}
```

Key validation rules:

- `fullName` required, non-empty.
- `phoneNumber` required, non-empty.
- At least one room type in `roomTypes`.
- `checkInDate > DateTime.UtcNow` (strictly in the future – note: this is slightly different from logged-in booking logic using `UtcNow.Date`).
- `checkOutDate > checkInDate`.

Customer handling:

- If `email` is empty:
  - Try to find existing `Customer` by `phoneNumber`.
- If `email` is provided:
  - Try to find existing `Customer` by `phoneNumber` or linked `Account.Email`.
- If customer exists:
  - Update name/identityCard/address if different.
- If customer does not exist:
  - If `email` is provided:
    - Check if an `Account` with that email exists.
    - If not, create a new `Account` for guest:
      - Random strong password (hashed with BCrypt).
      - Assign `User` role via `Roles.AddAccountRoleAsync`.
  - Create new `Customer` linked to the account (if created).

Room & pricing logic is the same as user booking:

- Find available rooms by room type and date range.
- Calculate `totalAmount` and `depositAmount = totalAmount * 0.3`.
- Set status `Pending` and type `Online`.
- Create `Booking` and `BookingRoom` records.
- Generate QR payment info (with try/catch – booking still valid if QR generation fails).
- Cache payment info and schedule timeout.
- Generate booking token.

### Successful response (201 Created)

Same structure as `CreateBooking` response:

```jsonc
{
  "isSuccess": true,
  "statusCode": 201,
  "message": "Tạo booking thành công. Vui lòng quét mã QR để thanh toán trong 15 phút!",
  "data": {
    "booking": { /* BookingDto, similar to logged-in user */ },
    "bookingToken": "{token}",
    "qrPayment": { /* may be null if QR generation failed */ },
    "paymentDeadline": "2025-12-09T10:15:00Z"
  }
}
```

Error responses follow the same patterns as `CreateBookingAsync`, with guest-specific messages logged but not exposed to client.

---

## 4. Get booking by ID (authorized)

**Endpoint**  
`GET /api/Booking/{bookingId}`  
Auth: **[Authorize]**

### Behavior

- Looks up `Booking` by `bookingId`.
- Loads `Customer`, associated `BookingRoom` records and `Room` entities.
- Groups rooms by `RoomType` to construct `RoomTypeQuantityDto` list.
- Resolves status and booking type via `CommonCode`.
- Returns a `BookingDto` in `data`.

### Successful response (200 OK)

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "bookingId": 123,
    "customerId": 10,
    "customerName": "Nguyễn Văn A",
    "roomIds": [101, 102],
    "roomNames": ["101", "102"],
    "roomTypeDetails": [
      {
        "roomTypeId": 1,
        "roomTypeName": "Standard",
        "roomTypeCode": "STD",
        "quantity": 2,
        "pricePerNight": 500000,
        "subTotal": 0 // not set in GetBookingByIdAsync
      }
    ],
    "checkInDate": "2025-12-20T14:00:00Z",
    "checkOutDate": "2025-12-22T12:00:00Z",
    "totalAmount": 2000000,
    "depositAmount": 600000,
    "paymentStatus": "Pending",   // from CommonCode.CodeValue
    "bookingType": "Online",      // from CommonCode.CodeValue
    "specialRequests": "...",
    "createdAt": "2025-12-09T10:00:00Z",
    "paymentStatus": "Pending",
    "depositStatus": "",          // not filled
    "paymentUrl": null,
    "orderCode": "TRX-..."        // if available
  }
}
```

If booking is not found:

```jsonc
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy booking"
}
```

---

## 5. Get booking by token (guest)

**Endpoint**  
`GET /api/Booking/mybooking/{token}`  
Auth: **AllowAnonymous**

### Behavior

- Decodes `token` to `bookingId` via `BookingTokenHelper.DecodeBookingToken`.
- Delegates to `GetBookingByIdAsync(bookingId)`.

### Responses

- If token is valid and booking exists → same as **Get booking by ID**.
- If token is invalid:

```jsonc
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Token không hợp lệ: {errorMessage}"
}
```

---

## 6. Notify payment / Cancel booking (ConfirmPayment)

**Endpoint**  
`POST /api/Booking/confirm-payment`  
Auth: **AllowAnonymous** in controller, but `UserId` from current token is attached if present.

Controller logic:

```csharp
[HttpPost("confirm-payment")]
[AllowAnonymous]
public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
{
    var userId = CurrentUserId; // 0 if unauthenticated
    request.UserId = userId;
    var result = await _bookingService.ProcessPaymentAsync(request);
    return StatusCode(result.StatusCode, result);
}
```

### Request body

Model: `ConfirmPaymentRequest`

```jsonc
{
  "bookingId": 123,
  "isCancel": false,
  "cancellationReason": "Khách đã chuyển khoản" // optional, used only for logging/UI
}
```

- `bookingId` (int) – required.
- `isCancel` (bool) –
  - `true`: treat as **cancel booking**.
  - `false`: treat as **user has paid** → move to `PendingConfirmation`.
- `userId` is set server-side from JWT and not required from client.

### Behavior – Cancel booking path (`isCancel = true`)

1. Load `Booking` by `bookingId`.
2. Resolve status `BookingStatus = Cancelled` from `CommonCode`.
3. Set `booking.StatusId = Cancelled` and `UpdatedAt = UtcNow`.
4. Save changes.
5. Remove cache entry `BookingPayment:{bookingId}`.

**Response example:**

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Đã hủy booking thành công."
}
```

If booking not found:

```jsonc
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy booking"
}
```

### Behavior – Notify payment path (`isCancel = false`)

1. Load `Booking` by `bookingId`.
2. Resolve status `BookingStatus = PendingConfirmation` from `CommonCode`.
3. Set `booking.StatusId = PendingConfirmation` and `UpdatedAt = UtcNow`.
4. Save changes.
5. Send email to manager via `IEmailService.SendPaymentConfirmationRequestEmailToManagerAsync(bookingId)`.
6. Do **not** change cache here; timeout cancellation / manual confirmation still apply.

**Response example:**

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Cảm ơn bạn đã thông báo thanh toán. Quản lý sẽ kiểm tra bill ngân hàng và xác nhận trong thời gian sớm nhất."
}
```

If `PendingConfirmation` status code is missing from `CommonCode`:

```jsonc
{
  "isSuccess": false,
  "statusCode": 500,
  "message": "Lỗi: Không tìm thấy status PendingConfirmation"
}
```

---

## 7. Cancel booking (authorized shortcut)

**Endpoint**  
`DELETE /api/Booking/{bookingId}`  
Auth: **[Authorize]**

Controller behavior:

- Wraps the call to `ProcessPaymentAsync` with `isCancel = true` and `UserId = CurrentUserId`:

```csharp
[HttpDelete("{bookingId}")]
[Authorize]
public async Task<IActionResult> CancelBooking(int bookingId)
{
    var userId = CurrentUserId;
    var request = new ConfirmPaymentRequest 
    { 
        BookingId = bookingId, 
        IsCancel = true, 
        UserId = userId 
    };
    var result = await _bookingService.ProcessPaymentAsync(request);
    return StatusCode(result.StatusCode, result);
}
```

Responses are the same as the **cancel booking** path in `ProcessPaymentAsync`.

---

## 8. Get bookings of current account

**Endpoint**  
`GET /api/Booking/my-bookings`  
Auth: **[Authorize]**

- Uses `CurrentUserId` as `accountId` and calls `_bookingService.GetMyBookingsByAccountIdAsync(accountId)`.
- Returns a list/paged list of bookings belonging to the current account (implementation not fully listed here but follows `ResultModel` pattern).

Typical response shape:

```jsonc
{
  "isSuccess": true,
  "statusCode": 200,
  "data": [
    {
      "bookingId": 123,
      "checkInDate": "2025-12-20T14:00:00Z",
      "checkOutDate": "2025-12-22T12:00:00Z",
      "totalAmount": 3500000,
      "depositAmount": 1050000,
      "paymentStatus": "Pending",
      "bookingType": "Online"
      // plus other fields depending on DTO implementation
    }
  ]
}
```

---

## High-level client flow summary

1. **Step 1 – Check availability**
   - Call `POST /api/Booking/check-availability` with room types and dates.
   - If `statusCode = 200` and `data.isAllAvailable = true` → cho phép user sang bước 2.
   - Nếu `statusCode = 409` → hiển thị chi tiết loại phòng nào thiếu.

2. **Step 2 – Create booking**
   - Logged-in user: `POST /api/Booking`.
   - Guest: `POST /api/Booking/guest`.
   - Backend sẽ tự:
     - Chọn phòng cụ thể.
     - Tạo booking & booking rooms.
     - Generate QR thanh toán đặt cọc.
     - Cache thông tin thanh toán & schedule auto-timeout.

3. **Step 3 – Thanh toán & xác nhận**
   - Frontend hiển thị QR (`data.qrPayment`) + countdown 15 phút.
   - Sau khi khách chuyển khoản, có 2 option:
     - Gọi `POST /api/Booking/confirm-payment` với `isCancel = false` để báo "đã thanh toán".
     - Hoặc bỏ qua, để hệ thống auto-cancel khi hết hạn (theo `BookingTimeoutChecker`).

4. **Step 4 – Hủy booking**
   - User muốn hủy manual:
     - Logged-in: `DELETE /api/Booking/{bookingId}`.
     - Hoặc gọi `POST /api/Booking/confirm-payment` với `isCancel = true`.
   - Hệ thống set status = `Cancelled` và xóa cache payment.

5. **Step 5 – Xem booking**
   - Logged-in user: `GET /api/Booking/{bookingId}` hoặc `GET /api/Booking/my-bookings`.
   - Guest: sử dụng link chứa `bookingToken` → `GET /api/Booking/mybooking/{token}`.

