# Check-In API - Request & Response Documentation

## API Endpoint
```
POST /api/BookingManagement/{bookingId}/check-in
```

## Authorization
- **Required**: Yes
- **Roles**: `Receptionist`, `Manager`, `Admin`
- **Header**: `Authorization: Bearer {token}`

---

## Request

### URL Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bookingId` | `int` | ✅ Yes | ID của booking cần check-in |

### Headers
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

### Request Body
**None** - This endpoint doesn't require a request body. The `employeeId` is automatically extracted from the JWT token.

### Example Request
```http
POST /api/BookingManagement/11/check-in HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

---

## Response

### Success Response (200 OK)

#### Response Model Structure
```json
{
    "isSuccess": true,
    "responseCode": "SUCCESS",
    "statusCode": 200,
    "data": {
        "bookingId": 11,
        "customer": {
            "customerId": 1,
            "fullName": "nam",
            "email": "namdnhe176906@fpt.edu.vn",
            "phoneNumber": "0987654321",
            "identityCard": "0987654321"
        },
        "checkInDate": "2025-12-17T17:00:00",
        "checkOutDate": "2025-12-25T17:00:00",
        "bookingStatus": "Đã nhận phòng",
        "bookingStatusCode": "CheckedIn",
        "bookingType": "Đặt trực tuyến",
        "bookingTypeCode": "Online",
        "rooms": [
            {
                "bookingRoomId": 40,
                "roomId": 11,
                "roomName": "201",
                "roomTypeName": "Phòng Cao Cấp",
                "roomTypeCode": "DLX",
                "pricePerNight": 1500000.00,
                "numberOfNights": 8,
                "subTotal": 12000000.00,
                "checkInDate": "2025-12-17T17:00:00",
                "checkOutDate": "2025-12-25T17:00:00"
            }
        ],
        "totalAmount": 12000000.00,
        "depositAmount": 3600000.00,
        "checkedInAt": "2025-12-17T10:30:45.123Z",
        "checkedInBy": 5
    },
    "message": "Check-in thành công!"
}
```

#### Response Fields Description

| Field Path | Type | Description |
|------------|------|-------------|
| `isSuccess` | `boolean` | Trạng thái thành công của request |
| `responseCode` | `string` | Mã response (SUCCESS, ERROR, etc.) |
| `statusCode` | `int` | HTTP status code |
| `message` | `string` | Thông báo kết quả |
| **`data`** | `object` | Dữ liệu check-in |
| `data.bookingId` | `int` | ID của booking |
| **`data.customer`** | `object` | Thông tin khách hàng |
| `data.customer.customerId` | `int` | ID khách hàng |
| `data.customer.fullName` | `string` | Họ tên khách hàng |
| `data.customer.email` | `string` | Email khách hàng |
| `data.customer.phoneNumber` | `string` | Số điện thoại |
| `data.customer.identityCard` | `string` | Số CMND/CCCD |
| `data.checkInDate` | `datetime` | Ngày check-in dự kiến |
| `data.checkOutDate` | `datetime` | Ngày check-out dự kiến |
| `data.bookingStatus` | `string` | Trạng thái booking (Tiếng Việt) |
| `data.bookingStatusCode` | `string` | Mã trạng thái booking (English) |
| `data.bookingType` | `string` | Loại booking (Tiếng Việt) |
| `data.bookingTypeCode` | `string` | Mã loại booking (English) |
| **`data.rooms`** | `array` | Danh sách phòng trong booking |
| `data.rooms[].bookingRoomId` | `int` | ID của booking room |
| `data.rooms[].roomId` | `int` | ID của phòng |
| `data.rooms[].roomName` | `string` | Tên/Số phòng |
| `data.rooms[].roomTypeName` | `string` | Tên loại phòng |
| `data.rooms[].roomTypeCode` | `string` | Mã loại phòng |
| `data.rooms[].pricePerNight` | `decimal` | Giá mỗi đêm |
| `data.rooms[].numberOfNights` | `int` | Số đêm |
| `data.rooms[].subTotal` | `decimal` | Tổng tiền phòng |
| `data.rooms[].checkInDate` | `datetime` | Ngày check-in phòng |
| `data.rooms[].checkOutDate` | `datetime` | Ngày check-out phòng |
| `data.totalAmount` | `decimal` | Tổng tiền booking |
| `data.depositAmount` | `decimal` | Tiền cọc đã trả (30% nếu online) |
| `data.checkedInAt` | `datetime` | Thời gian check-in thực tế |
| `data.checkedInBy` | `int` | ID nhân viên xử lý check-in |

---

### Error Responses

#### 1. Booking Not Found (404)
```json
{
    "isSuccess": false,
    "responseCode": "NOT_FOUND",
    "statusCode": 404,
    "data": null,
    "message": "Không tìm thấy booking"
}
```

#### 2. Booking Already Checked In (400)
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking đã được check-in trước đó"
}
```

#### 3. Booking Not Confirmed Yet (400)
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking chưa được xác nhận, không thể check-in"
}
```

**Booking Status Flow:**
- `Pending` → Chờ thanh toán (chưa thể check-in)
- `PendingConfirmation` → Chờ xác nhận tiền cọc (chưa thể check-in)
- `Confirmed` → Đã xác nhận ✅ **CÓ THỂ CHECK-IN**
- `CheckedIn` → Đã nhận phòng (không thể check-in lại)
- `Completed` → Đã hoàn thành (đã checkout)
- `Cancelled` → Đã hủy

#### 4. Booking Cancelled (400)
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking đã bị hủy, không thể check-in"
}
```

#### 5. Booking Already Completed (400)
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking đã hoàn tất (đã checkout), không thể check-in"
}
```

#### 6. Unauthorized (401)
```json
{
    "isSuccess": false,
    "responseCode": "UNAUTHORIZED",
    "statusCode": 401,
    "data": null,
    "message": "Token không hợp lệ hoặc đã hết hạn"
}
```

#### 7. Forbidden (403)
```json
{
    "isSuccess": false,
    "responseCode": "FORBIDDEN",
    "statusCode": 403,
    "data": null,
    "message": "Bạn không có quyền thực hiện chức năng này"
}
```

**Roles có quyền:**
- ✅ `Receptionist` (Lễ tân)
- ✅ `Manager` (Quản lý)
- ✅ `Admin` (Quản trị viên)

#### 8. Server Error (500)
```json
{
    "isSuccess": false,
    "responseCode": "SERVER_ERROR",
    "statusCode": 500,
    "data": null,
    "message": "Lỗi khi xử lý check-in: [Chi tiết lỗi]"
}
```

---

## Business Logic

### What Happens During Check-In?

1. **Validate Booking**
   - Kiểm tra booking tồn tại
   - Kiểm tra trạng thái booking = `Confirmed` (đã xác nhận)
   - Không được check-in nếu:
     - Booking đang `Pending` hoặc `PendingConfirmation`
     - Booking đã `CheckedIn` trước đó
     - Booking đã `Completed` (đã checkout)
     - Booking đã `Cancelled`

2. **Update Booking Status**
   - Chuyển trạng thái từ `Confirmed` → `CheckedIn`
   - Ghi nhận thời gian check-in thực tế
   - Ghi nhận nhân viên xử lý (từ JWT token)

3. **Update Room Status**
   - Chuyển tất cả phòng trong booking từ `Available` → `Occupied`
   - Đánh dấu phòng đang có khách

4. **Transaction**
   - Check-in **KHÔNG TẠO** transaction mới
   - Transaction deposit (30%) đã được tạo khi xác nhận booking
   - Transaction checkout (100%) sẽ được tạo khi checkout

---

## Usage Examples

### Example 1: Check-In Booking Online (Đã Xác Nhận)

**Request:**
```bash
curl -X POST "http://localhost:8080/api/BookingManagement/11/check-in" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

**Response (200 OK):**
```json
{
    "isSuccess": true,
    "responseCode": "SUCCESS",
    "statusCode": 200,
    "data": {
        "bookingId": 11,
        "customer": {
            "customerId": 1,
            "fullName": "Nguyễn Văn Nam",
            "email": "namdnhe176906@fpt.edu.vn",
            "phoneNumber": "0987654321",
            "identityCard": "0987654321"
        },
        "checkInDate": "2025-12-17T17:00:00",
        "checkOutDate": "2025-12-25T17:00:00",
        "bookingStatus": "Đã nhận phòng",
        "bookingStatusCode": "CheckedIn",
        "bookingType": "Đặt trực tuyến",
        "bookingTypeCode": "Online",
        "rooms": [
            {
                "bookingRoomId": 40,
                "roomId": 11,
                "roomName": "201",
                "roomTypeName": "Phòng Cao Cấp",
                "roomTypeCode": "DLX",
                "pricePerNight": 1500000.00,
                "numberOfNights": 8,
                "subTotal": 12000000.00,
                "checkInDate": "2025-12-17T17:00:00",
                "checkOutDate": "2025-12-25T17:00:00"
            }
        ],
        "totalAmount": 12000000.00,
        "depositAmount": 3600000.00,
        "checkedInAt": "2025-12-17T10:30:45.123Z",
        "checkedInBy": 5
    },
    "message": "Check-in thành công!"
}
```

---

### Example 2: Check-In Failed - Not Confirmed Yet

**Request:**
```bash
curl -X POST "http://localhost:8080/api/BookingManagement/12/check-in" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

**Response (400 Bad Request):**
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking chưa được xác nhận, không thể check-in"
}
```

**Reason:** Booking vẫn đang ở trạng thái `PendingConfirmation` (chờ manager xác nhận tiền cọc)

---

### Example 3: Check-In Failed - Already Checked In

**Request:**
```bash
curl -X POST "http://localhost:8080/api/BookingManagement/11/check-in" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

**Response (400 Bad Request):**
```json
{
    "isSuccess": false,
    "responseCode": "BAD_REQUEST",
    "statusCode": 400,
    "data": null,
    "message": "Booking đã được check-in trước đó"
}
```

**Reason:** Booking đã ở trạng thái `CheckedIn`, không thể check-in lại

---

## Integration Notes

### Frontend Integration

```typescript
// TypeScript example
interface CheckInRequest {
  bookingId: number;
}

interface CheckInResponse {
  isSuccess: boolean;
  responseCode: string;
  statusCode: number;
  data: {
    bookingId: number;
    customer: {
      customerId: number;
      fullName: string;
      email: string;
      phoneNumber: string;
      identityCard: string;
    };
    checkInDate: string;
    checkOutDate: string;
    bookingStatus: string;
    bookingStatusCode: string;
    bookingType: string;
    bookingTypeCode: string;
    rooms: Array<{
      bookingRoomId: number;
      roomId: number;
      roomName: string;
      roomTypeName: string;
      roomTypeCode: string;
      pricePerNight: number;
      numberOfNights: number;
      subTotal: number;
      checkInDate: string;
      checkOutDate: string;
    }>;
    totalAmount: number;
    depositAmount: number;
    checkedInAt: string;
    checkedInBy: number;
  };
  message: string;
}

async function checkInBooking(bookingId: number): Promise<CheckInResponse> {
  const response = await fetch(`/api/BookingManagement/${bookingId}/check-in`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`,
      'Content-Type': 'application/json'
    }
  });
  
  return await response.json();
}

// Usage
try {
  const result = await checkInBooking(11);
  if (result.isSuccess) {
    console.log('Check-in thành công!', result.data);
    // Update UI, show success message
  } else {
    console.error('Check-in thất bại:', result.message);
    // Show error message
  }
} catch (error) {
  console.error('Network error:', error);
}
```

---

## Testing

### Test Cases

1. ✅ **Happy Path: Check-In Confirmed Booking**
   - Status: `Confirmed` → `CheckedIn`
   - Room Status: `Available` → `Occupied`
   
2. ❌ **Booking Not Found**
   - BookingId không tồn tại → 404

3. ❌ **Booking Pending Confirmation**
   - Status: `PendingConfirmation` → Cannot check-in → 400

4. ❌ **Booking Already Checked In**
   - Status: `CheckedIn` → Cannot check-in again → 400

5. ❌ **Booking Cancelled**
   - Status: `Cancelled` → Cannot check-in → 400

6. ❌ **Unauthorized**
   - No token hoặc token invalid → 401

7. ❌ **Forbidden**
   - Role không phải Receptionist/Manager/Admin → 403

---

## Related APIs

- **GET /api/BookingManagement/{bookingId}** - Get booking details
- **POST /api/BookingManagement/{bookingId}/confirm** - Confirm booking (before check-in)
- **GET /api/Checkout/preview/{bookingId}** - Preview checkout invoice
- **POST /api/Checkout/process** - Process checkout
- **GET /api/Dashboard/stats** - Get dashboard statistics (includes checked-in bookings)

---

## Changelog

### Version 1.0 (2025-12-17)
- ✅ Initial API documentation
- ✅ Request/Response structure defined
- ✅ Error handling documented
- ✅ Business logic explained
- ✅ Frontend integration examples added

