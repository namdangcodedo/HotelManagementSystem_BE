# Checkout API Documentation

## 📚 Overview

Tài liệu này mô tả chi tiết các API endpoints của Checkout module trong hệ thống quản lý khách sạn. Module này xử lý quy trình thanh toán và hoàn tất booking.

**Base URL:** `/api/Checkout`

**Authentication:** Tất cả endpoints yêu cầu Bearer Token

---

## 📊 API Endpoints

### 1. GET /api/Checkout/preview/{bookingId}

**Priority:** ✅ **CAO** - Required

**Description:** Preview hóa đơn checkout (không lưu DB) - Xem trước chi tiết thanh toán trước khi thực hiện checkout

**Use Case:**
- Hiển thị preview hóa đơn cho khách hàng trước khi thanh toán
- Tính toán chi phí khi checkout sớm/muộn so với dự kiến
- Kiểm tra các khoản phí phòng và dịch vụ đã sử dụng

**Authorization:** Yêu cầu đăng nhập (Authorize)

#### Request

```http
GET /api/Checkout/preview/{bookingId}?estimatedCheckOutDate=2024-01-20T12:00:00 HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
Content-Type: application/json
```

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bookingId` | integer | Yes | ID của booking cần checkout |

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `estimatedCheckOutDate` | datetime | No | null | Ngày checkout dự kiến (ISO 8601 format) để tính tiền nếu checkout sớm/muộn |

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Response

**Success Response (200 OK):**

```json
{
  "data": {
    "bookingId": 123,
    "bookingType": "Online",
    "customer": {
      "customerId": 45,
      "fullName": "Nguyễn Văn A",
      "email": "nguyenvana@example.com",
      "phoneNumber": "0912345678",
      "identityCard": "001234567890"
    },
    "checkInDate": "2024-01-15T14:00:00",
    "checkOutDate": "2024-01-20T12:00:00",
    "totalNights": 5,
    "estimatedCheckOutDate": "2024-01-20T12:00:00",
    "estimatedNights": 5,
    "roomCharges": [
      {
        "bookingRoomId": 1,
        "roomId": 101,
        "roomName": "P101",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4250000,
        "checkInDate": "2024-01-15T14:00:00",
        "checkOutDate": "2024-01-20T12:00:00"
      },
      {
        "bookingRoomId": 2,
        "roomId": 102,
        "roomName": "P102",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4250000,
        "checkInDate": "2024-01-15T14:00:00",
        "checkOutDate": "2024-01-20T12:00:00"
      }
    ],
    "totalRoomCharges": 8500000,
    "serviceCharges": [
      {
        "serviceId": 1,
        "serviceName": "Massage",
        "pricePerUnit": 300000,
        "quantity": 2,
        "subTotal": 600000,
        "serviceDate": "2024-01-16T10:00:00",
        "serviceType": "RoomService",
        "roomName": "P101"
      },
      {
        "serviceId": 2,
        "serviceName": "Giặt ủi",
        "pricePerUnit": 50000,
        "quantity": 3,
        "subTotal": 150000,
        "serviceDate": "2024-01-17T09:00:00",
        "serviceType": "RoomService",
        "roomName": "P102"
      }
    ],
    "totalServiceCharges": 750000,
    "subTotal": 9250000,
    "depositPaid": 2000000,
    "totalAmount": 9250000,
    "amountDue": 7250000,
    "message": null
  },
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Preview checkout successfully"
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | object | Preview checkout data |
| `data.bookingId` | integer | ID của booking |
| `data.bookingType` | string | Loại booking: "Online" hoặc "WalkIn" |
| `data.customer` | object | Thông tin khách hàng |
| `data.customer.customerId` | integer | ID khách hàng |
| `data.customer.fullName` | string | Họ tên đầy đủ |
| `data.customer.email` | string | Email |
| `data.customer.phoneNumber` | string | Số điện thoại |
| `data.customer.identityCard` | string | Số CMND/CCCD |
| `data.checkInDate` | datetime | Ngày check-in |
| `data.checkOutDate` | datetime | Ngày check-out dự kiến ban đầu |
| `data.totalNights` | integer | Tổng số đêm ban đầu |
| `data.estimatedCheckOutDate` | datetime | Ngày checkout ước tính (từ query param) |
| `data.estimatedNights` | integer | Số đêm ước tính |
| `data.roomCharges` | array | Danh sách chi tiết tiền phòng |
| `data.roomCharges[].bookingRoomId` | integer | ID booking room |
| `data.roomCharges[].roomId` | integer | ID phòng |
| `data.roomCharges[].roomName` | string | Tên/số phòng |
| `data.roomCharges[].roomTypeName` | string | Loại phòng |
| `data.roomCharges[].pricePerNight` | decimal | Giá mỗi đêm (VNĐ) |
| `data.roomCharges[].plannedNights` | integer | Số đêm dự kiến |
| `data.roomCharges[].actualNights` | integer | Số đêm thực tế |
| `data.roomCharges[].subTotal` | decimal | Tổng tiền phòng (VNĐ) |
| `data.totalRoomCharges` | decimal | Tổng tiền tất cả phòng (VNĐ) |
| `data.serviceCharges` | array | Danh sách chi tiết dịch vụ |
| `data.serviceCharges[].serviceId` | integer | ID dịch vụ |
| `data.serviceCharges[].serviceName` | string | Tên dịch vụ |
| `data.serviceCharges[].pricePerUnit` | decimal | Giá đơn vị (VNĐ) |
| `data.serviceCharges[].quantity` | integer | Số lượng |
| `data.serviceCharges[].subTotal` | decimal | Tổng tiền dịch vụ (VNĐ) |
| `data.serviceCharges[].serviceType` | string | Loại: "RoomService" hoặc "BookingService" |
| `data.serviceCharges[].roomName` | string | Tên phòng (nếu là dịch vụ theo phòng) |
| `data.totalServiceCharges` | decimal | Tổng tiền dịch vụ (VNĐ) |
| `data.subTotal` | decimal | Tổng cộng trước cọc (VNĐ) |
| `data.depositPaid` | decimal | Tiền cọc đã trả (VNĐ) |
| `data.totalAmount` | decimal | Tổng hóa đơn (VNĐ) |
| `data.amountDue` | decimal | Còn phải trả (VNĐ) |
| `data.message` | string | Cảnh báo/thông báo (nếu có) |

**Error Response (400 Bad Request):**

```json
{
  "isSuccess": false,
  "responseCode": "BAD_REQUEST",
  "statusCode": 400,
  "data": null,
  "message": "Booking ID không hợp lệ"
}
```

**Error Response (404 Not Found):**

```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "data": null,
  "message": "Không tìm thấy booking"
}
```

**Error Response (401 Unauthorized):**

```json
{
  "isSuccess": false,
  "responseCode": "UNAUTHORIZED",
  "statusCode": 401,
  "data": null,
  "message": "Unauthorized access"
}
```

#### Business Logic

- **Amount Due:** `totalAmount - depositPaid`
- **SubTotal:** `totalRoomCharges + totalServiceCharges`
- **Room SubTotal:** `pricePerNight × actualNights`
- **Service SubTotal:** `pricePerUnit × quantity`
- Nếu `estimatedCheckOutDate` được cung cấp và khác `checkOutDate`, hệ thống sẽ tính lại `actualNights` và cập nhật giá

#### Example Usage

**cURL:**
```bash
curl -X GET "http://localhost:8080/api/Checkout/preview/123?estimatedCheckOutDate=2024-01-20T12:00:00" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

**JavaScript (Fetch):**
```javascript
const bookingId = 123;
const estimatedDate = '2024-01-20T12:00:00';

const response = await fetch(
  `http://localhost:8080/api/Checkout/preview/${bookingId}?estimatedCheckOutDate=${estimatedDate}`,
  {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json'
    }
  }
);

const data = await response.json();
```

**TypeScript:**
```typescript
interface PreviewCheckoutResponse {
  bookingId: number;
  bookingType: string;
  customer: CustomerCheckoutInfo;
  checkInDate: string;
  checkOutDate: string;
  totalNights: number;
  estimatedCheckOutDate?: string;
  estimatedNights?: number;
  roomCharges: RoomChargeDetail[];
  totalRoomCharges: number;
  serviceCharges: ServiceChargeDetail[];
  totalServiceCharges: number;
  subTotal: number;
  depositPaid: number;
  totalAmount: number;
  amountDue: number;
  message?: string;
}

const { data } = await fetch(`/api/Checkout/preview/${bookingId}`, {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(res => res.json());
```

---

### 2. POST /api/Checkout

**Priority:** ✅ **CAO NHẤT** - Required

**Description:** Xử lý checkout và thanh toán hoàn tất - Thực hiện thanh toán, tạo transaction và hoàn tất booking

**Use Case:**
- Thực hiện checkout và thanh toán cho khách
- Tạo transaction ghi nhận thanh toán
- Cập nhật trạng thái booking thành "Completed"
- Cập nhật trạng thái phòng

**Authorization:** Yêu cầu role `Receptionist`, `Manager`, hoặc `Admin`

#### Request

```http
POST /api/Checkout HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "bookingId": 123,
  "actualCheckOutDate": "2024-01-20T12:00:00",
  "paymentMethodId": 15,
  "paymentNote": "Thanh toán bằng tiền mặt",
  "transactionReference": null
}
```

**Request Body:**

```json
{
  "bookingId": 123,
  "actualCheckOutDate": "2024-01-20T12:00:00",
  "paymentMethodId": 15,
  "paymentNote": "Thanh toán bằng tiền mặt",
  "transactionReference": null
}
```

**Request Schema:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `bookingId` | integer | Yes | ID của booking cần checkout |
| `actualCheckOutDate` | datetime | Yes | Ngày checkout thực tế (ISO 8601 format) |
| `paymentMethodId` | integer | Yes | ID phương thức thanh toán (từ CommonCode) |
| `paymentNote` | string | No | Ghi chú thanh toán |
| `transactionReference` | string | No | Mã giao dịch tham chiếu (nếu thanh toán qua bank) |

**Payment Method IDs:**
- Cash: Check CommonCode table
- Card: Check CommonCode table
- QR: Check CommonCode table
- PayOS: Check CommonCode table

#### Response

**Success Response (200 OK):**

```json
{
  "data": {
    "bookingId": 123,
    "bookingType": "Online",
    "customer": {
      "customerId": 45,
      "fullName": "Nguyễn Văn A",
      "email": "nguyenvana@example.com",
      "phoneNumber": "0912345678",
      "identityCard": "001234567890"
    },
    "checkInDate": "2024-01-15T14:00:00",
    "checkOutDate": "2024-01-20T12:00:00",
    "actualCheckOutDate": "2024-01-20T12:00:00",
    "totalNights": 5,
    "actualNights": 5,
    "roomCharges": [
      {
        "bookingRoomId": 1,
        "roomId": 101,
        "roomName": "P101",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4250000,
        "checkInDate": "2024-01-15T14:00:00",
        "checkOutDate": "2024-01-20T12:00:00"
      },
      {
        "bookingRoomId": 2,
        "roomId": 102,
        "roomName": "P102",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000,
        "plannedNights": 5,
        "actualNights": 5,
        "subTotal": 4250000,
        "checkInDate": "2024-01-15T14:00:00",
        "checkOutDate": "2024-01-20T12:00:00"
      }
    ],
    "totalRoomCharges": 8500000,
    "serviceCharges": [
      {
        "serviceId": 1,
        "serviceName": "Massage",
        "pricePerUnit": 300000,
        "quantity": 2,
        "subTotal": 600000,
        "serviceDate": "2024-01-16T10:00:00",
        "serviceType": "RoomService",
        "roomName": "P101"
      },
      {
        "serviceId": 2,
        "serviceName": "Giặt ủi",
        "pricePerUnit": 50000,
        "quantity": 3,
        "subTotal": 150000,
        "serviceDate": "2024-01-17T09:00:00",
        "serviceType": "RoomService",
        "roomName": "P102"
      }
    ],
    "totalServiceCharges": 750000,
    "subTotal": 9250000,
    "depositPaid": 2000000,
    "totalAmount": 9250000,
    "amountDue": 7250000,
    "paymentMethod": "Cash",
    "transactionId": 456,
    "checkoutProcessedAt": "2024-01-20T12:05:30",
    "processedBy": "Nguyễn Thị B (Receptionist)"
  },
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Checkout completed successfully"
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | object | Checkout result data |
| `data.bookingId` | integer | ID của booking |
| `data.bookingType` | string | Loại booking |
| `data.customer` | object | Thông tin khách hàng |
| `data.checkInDate` | datetime | Ngày check-in |
| `data.checkOutDate` | datetime | Ngày check-out dự kiến |
| `data.actualCheckOutDate` | datetime | Ngày check-out thực tế |
| `data.totalNights` | integer | Tổng số đêm dự kiến |
| `data.actualNights` | integer | Số đêm thực tế |
| `data.roomCharges` | array | Chi tiết tiền phòng |
| `data.totalRoomCharges` | decimal | Tổng tiền phòng (VNĐ) |
| `data.serviceCharges` | array | Chi tiết dịch vụ |
| `data.totalServiceCharges` | decimal | Tổng tiền dịch vụ (VNĐ) |
| `data.subTotal` | decimal | Tổng cộng (VNĐ) |
| `data.depositPaid` | decimal | Tiền cọc đã trả (VNĐ) |
| `data.totalAmount` | decimal | Tổng hóa đơn (VNĐ) |
| `data.amountDue` | decimal | Còn phải trả (VNĐ) |
| `data.paymentMethod` | string | Tên phương thức thanh toán |
| `data.transactionId` | integer | ID transaction được tạo |
| `data.checkoutProcessedAt` | datetime | Thời gian xử lý checkout |
| `data.processedBy` | string | Nhân viên xử lý |

**Error Response (400 Bad Request):**

```json
{
  "isSuccess": false,
  "responseCode": "BAD_REQUEST",
  "statusCode": 400,
  "data": null,
  "message": "Request không hợp lệ"
}
```

**Error Response (400 - Validation):**

```json
{
  "isSuccess": false,
  "responseCode": "VALIDATION_ERROR",
  "statusCode": 400,
  "data": null,
  "message": "Booking ID không hợp lệ"
}
```

**Error Response (404 Not Found):**

```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "data": null,
  "message": "Không tìm thấy booking"
}
```

**Error Response (403 Forbidden):**

```json
{
  "isSuccess": false,
  "responseCode": "FORBIDDEN",
  "statusCode": 403,
  "data": null,
  "message": "Access denied. Require Receptionist, Manager or Admin role"
}
```

**Error Response (409 Conflict):**

```json
{
  "isSuccess": false,
  "responseCode": "CONFLICT",
  "statusCode": 409,
  "data": null,
  "message": "Booking đã được checkout trước đó"
}
```

#### Business Logic

1. **Validation:**
   - Kiểm tra booking tồn tại
   - Kiểm tra booking chưa được checkout
   - Kiểm tra payment method hợp lệ
   - Kiểm tra actual checkout date hợp lệ

2. **Calculation:**
   - Tính số đêm thực tế dựa trên `actualCheckOutDate`
   - Tính lại `totalRoomCharges` nếu checkout sớm/muộn
   - Cộng `totalServiceCharges`
   - Trừ `depositPaid` để tính `amountDue`

3. **Transaction Creation:**
   - Tạo transaction mới với:
     - `TotalAmount` = subTotal
     - `PaidAmount` = amountDue (số tiền khách trả)
     - `PaymentMethodId` = paymentMethodId từ request
     - `PaymentStatusId` = "Paid"

4. **Booking Update:**
   - Cập nhật booking status thành "Completed"
   - Cập nhật `ActualCheckOutDate`

5. **Room Status Update:**
   - Cập nhật tất cả phòng trong booking về trạng thái "Available"

#### Example Usage

**cURL:**
```bash
curl -X POST "http://localhost:8080/api/Checkout" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingId": 123,
    "actualCheckOutDate": "2024-01-20T12:00:00",
    "paymentMethodId": 15,
    "paymentNote": "Thanh toán bằng tiền mặt",
    "transactionReference": null
  }'
```

**JavaScript (Fetch):**
```javascript
const checkoutData = {
  bookingId: 123,
  actualCheckOutDate: '2024-01-20T12:00:00',
  paymentMethodId: 15,
  paymentNote: 'Thanh toán bằng tiền mặt',
  transactionReference: null
};

const response = await fetch('http://localhost:8080/api/Checkout', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify(checkoutData)
});

const result = await response.json();
if (result.isSuccess) {
  console.log('Checkout thành công!', result.data);
} else {
  console.error('Lỗi checkout:', result.message);
}
```

**TypeScript:**
```typescript
interface CheckoutRequest {
  bookingId: number;
  actualCheckOutDate: string;
  paymentMethodId: number;
  paymentNote?: string;
  transactionReference?: string;
}

interface CheckoutResponse {
  bookingId: number;
  bookingType: string;
  customer: CustomerCheckoutInfo;
  checkInDate: string;
  checkOutDate: string;
  actualCheckOutDate: string;
  totalNights: number;
  actualNights: number;
  roomCharges: RoomChargeDetail[];
  totalRoomCharges: number;
  serviceCharges: ServiceChargeDetail[];
  totalServiceCharges: number;
  subTotal: number;
  depositPaid: number;
  totalAmount: number;
  amountDue: number;
  paymentMethod: string;
  transactionId: number;
  checkoutProcessedAt: string;
  processedBy: string;
}

const processCheckout = async (request: CheckoutRequest): Promise<CheckoutResponse> => {
  const response = await fetch('/api/Checkout', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request)
  });

  const result = await response.json();
  if (!result.isSuccess) {
    throw new Error(result.message);
  }

  return result.data;
};
```

---

### 3. GET /api/Checkout/booking/{bookingId}

**Priority:** ⚠️ **TRUNG BÌNH** - Optional

**Description:** Lấy thông tin booking để chuẩn bị checkout - Dùng để load thông tin booking trước khi hiển thị màn hình checkout

**Use Case:**
- Load thông tin booking khi vào màn hình checkout
- Hiển thị thông tin khách hàng và phòng
- Kiểm tra trạng thái booking có thể checkout được không

**Authorization:** Yêu cầu đăng nhập (Authorize)

#### Request

```http
GET /api/Checkout/booking/123 HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
Content-Type: application/json
```

**Path Parameters:**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bookingId` | integer | Yes | ID của booking |

#### Response

**Success Response (200 OK):**

```json
{
  "data": {
    "bookingId": 123,
    "bookingType": "Online",
    "status": "Confirmed",
    "customer": {
      "customerId": 45,
      "fullName": "Nguyễn Văn A",
      "email": "nguyenvana@example.com",
      "phoneNumber": "0912345678",
      "identityCard": "001234567890"
    },
    "checkInDate": "2024-01-15T14:00:00",
    "checkOutDate": "2024-01-20T12:00:00",
    "rooms": [
      {
        "roomId": 101,
        "roomName": "P101",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000
      },
      {
        "roomId": 102,
        "roomName": "P102",
        "roomTypeName": "Deluxe",
        "pricePerNight": 850000
      }
    ],
    "totalAmount": 9250000,
    "depositPaid": 2000000,
    "canCheckout": true,
    "message": null
  },
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Get booking information successfully"
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data.bookingId` | integer | ID booking |
| `data.bookingType` | string | Loại booking |
| `data.status` | string | Trạng thái booking |
| `data.customer` | object | Thông tin khách hàng |
| `data.checkInDate` | datetime | Ngày check-in |
| `data.checkOutDate` | datetime | Ngày check-out dự kiến |
| `data.rooms` | array | Danh sách phòng |
| `data.totalAmount` | decimal | Tổng hóa đơn (VNĐ) |
| `data.depositPaid` | decimal | Tiền cọc (VNĐ) |
| `data.canCheckout` | boolean | Có thể checkout không |
| `data.message` | string | Thông báo/cảnh báo |

**Error Response (400 Bad Request):**

```json
{
  "isSuccess": false,
  "responseCode": "BAD_REQUEST",
  "statusCode": 400,
  "data": null,
  "message": "Booking ID không hợp lệ"
}
```

**Error Response (404 Not Found):**

```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "data": null,
  "message": "Không tìm thấy booking"
}
```

#### Example Usage

**cURL:**
```bash
curl -X GET "http://localhost:8080/api/Checkout/booking/123" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

**JavaScript:**
```javascript
const bookingId = 123;

const response = await fetch(`http://localhost:8080/api/Checkout/booking/${bookingId}`, {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  }
});

const { data } = await response.json();
console.log('Booking info:', data);
```

---

## 🔒 Authentication & Authorization

### Authentication

Tất cả endpoints yêu cầu JWT Bearer Token trong header:

```
Authorization: Bearer {access_token}
```

### Authorization Levels

| Endpoint | Required Roles | Description |
|----------|----------------|-------------|
| `GET /preview/{bookingId}` | Any authenticated user | Xem preview checkout |
| `POST /checkout` | Receptionist, Manager, Admin | Thực hiện checkout |
| `GET /booking/{bookingId}` | Any authenticated user | Xem thông tin booking |

---

## 📊 Response Format

Tất cả API responses đều follow cấu trúc chung:

```typescript
interface ApiResponse<T> {
  data: T;
  isSuccess: boolean;
  responseCode: string;
  statusCode: number;
  message: string;
}
```

### Response Codes

| Code | Description |
|------|-------------|
| `SUCCESS` | Request thành công |
| `UNAUTHORIZED` | Chưa đăng nhập hoặc token không hợp lệ |
| `FORBIDDEN` | Không có quyền truy cập |
| `NOT_FOUND` | Resource không tồn tại |
| `BAD_REQUEST` | Request không hợp lệ |
| `VALIDATION_ERROR` | Dữ liệu không hợp lệ |
| `CONFLICT` | Xung đột dữ liệu (booking đã checkout) |
| `SERVER_ERROR` | Lỗi server |

---

## 🎯 Checkout Flow - Quy trình thanh toán

### 1. Preview Checkout (Optional)
```
GET /api/Checkout/preview/{bookingId}
```
- Xem trước hóa đơn
- Tính toán chi phí
- Hiển thị breakdown phòng + dịch vụ

### 2. Confirm & Process Checkout
```
POST /api/Checkout
```
- Nhập thông tin thanh toán
- Chọn payment method
- Xử lý checkout
- Tạo transaction
- Cập nhật booking status

### 3. View Receipt (Optional)
- Sử dụng `CheckoutResponse` để hiển thị hóa đơn
- In hóa đơn
- Gửi email hóa đơn cho khách

---

## 🧮 Calculation Formulas

### Room Charges
```
roomSubTotal = pricePerNight × actualNights
totalRoomCharges = sum(all roomSubTotal)
```

### Service Charges
```
serviceSubTotal = pricePerUnit × quantity
totalServiceCharges = sum(all serviceSubTotal)
```

### Total Amount
```
subTotal = totalRoomCharges + totalServiceCharges
amountDue = subTotal - depositPaid
```

### Actual Nights
```
actualNights = days between checkInDate and actualCheckOutDate
```

---

## 💡 Best Practices

### 1. Error Handling

```typescript
try {
  const response = await fetch('/api/Checkout', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(checkoutRequest)
  });

  const result = await response.json();

  if (!result.isSuccess) {
    switch (result.responseCode) {
      case 'NOT_FOUND':
        showError('Không tìm thấy booking');
        break;
      case 'CONFLICT':
        showError('Booking đã được checkout trước đó');
        break;
      case 'FORBIDDEN':
        showError('Bạn không có quyền thực hiện checkout');
        break;
      default:
        showError(result.message);
    }
    return;
  }

  // Success
  showSuccess('Checkout thành công!');
  navigateToReceipt(result.data.transactionId);
} catch (error) {
  showError('Lỗi kết nối. Vui lòng thử lại.');
}
```

### 2. Preview Before Checkout

Luôn gọi preview trước khi checkout để khách xác nhận:

```typescript
// Step 1: Preview
const previewResponse = await fetch(`/api/Checkout/preview/${bookingId}`);
const preview = await previewResponse.json();

// Show preview to customer
showPreviewModal(preview.data);

// Step 2: Confirm and checkout
if (customerConfirmed) {
  const checkoutResponse = await fetch('/api/Checkout', {
    method: 'POST',
    body: JSON.stringify({
      bookingId,
      actualCheckOutDate: new Date().toISOString(),
      paymentMethodId: selectedPaymentMethod
    })
  });
}
```

### 3. Handle Early/Late Checkout

```typescript
const estimatedCheckOutDate = new Date();
const response = await fetch(
  `/api/Checkout/preview/${bookingId}?estimatedCheckOutDate=${estimatedCheckOutDate.toISOString()}`
);

const preview = await response.json();

if (preview.data.message) {
  // Show warning to user
  showWarning(preview.data.message);
}

// Display updated charges
displayCharges(preview.data);
```

---

## 📝 TypeScript Interfaces

```typescript
// Request Types
interface CheckoutRequest {
  bookingId: number;
  actualCheckOutDate: string; // ISO 8601
  paymentMethodId: number;
  paymentNote?: string;
  transactionReference?: string;
}

interface PreviewCheckoutRequest {
  bookingId: number;
  estimatedCheckOutDate?: string; // ISO 8601
}

// Response Types
interface CustomerCheckoutInfo {
  customerId: number;
  fullName: string;
  email: string;
  phoneNumber: string;
  identityCard?: string;
}

interface RoomChargeDetail {
  bookingRoomId: number;
  roomId: number;
  roomName: string;
  roomTypeName: string;
  pricePerNight: number;
  plannedNights: number;
  actualNights: number;
  subTotal: number;
  checkInDate: string;
  checkOutDate: string;
}

interface ServiceChargeDetail {
  serviceId: number;
  serviceName: string;
  pricePerUnit: number;
  quantity: number;
  subTotal: number;
  serviceDate: string;
  serviceType: 'RoomService' | 'BookingService';
  roomName?: string;
}

interface CheckoutResponse {
  bookingId: number;
  bookingType: string;
  customer: CustomerCheckoutInfo;
  checkInDate: string;
  checkOutDate: string;
  actualCheckOutDate: string;
  totalNights: number;
  actualNights: number;
  roomCharges: RoomChargeDetail[];
  totalRoomCharges: number;
  serviceCharges: ServiceChargeDetail[];
  totalServiceCharges: number;
  subTotal: number;
  depositPaid: number;
  totalAmount: number;
  amountDue: number;
  paymentMethod: string;
  transactionId: number;
  checkoutProcessedAt: string;
  processedBy: string;
}

interface PreviewCheckoutResponse {
  bookingId: number;
  bookingType: string;
  customer: CustomerCheckoutInfo;
  checkInDate: string;
  checkOutDate: string;
  totalNights: number;
  estimatedCheckOutDate?: string;
  estimatedNights?: number;
  roomCharges: RoomChargeDetail[];
  totalRoomCharges: number;
  serviceCharges: ServiceChargeDetail[];
  totalServiceCharges: number;
  subTotal: number;
  depositPaid: number;
  totalAmount: number;
  amountDue: number;
  message?: string;
}
```

---

## 📞 Support

Nếu có thắc mắc về API, vui lòng liên hệ:
- Backend Team
- Email: support@hotel-management.com

---

## 📝 Change Log

### Version 1.0.0 (2024-01-XX)
- ✅ Initial release
- ✅ Implemented `GET /preview/{bookingId}` endpoint
- ✅ Implemented `POST /checkout` endpoint
- ✅ Implemented `GET /booking/{bookingId}` endpoint

---

**Last Updated:** 2024-12-16
**API Version:** 1.0.0
**Backend:** ASP.NET Core 9.0
