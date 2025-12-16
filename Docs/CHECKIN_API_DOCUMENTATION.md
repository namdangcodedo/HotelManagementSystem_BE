# Check-In Booking API Documentation

## API Endpoint: Check-In Booking

**URL:** `POST /api/bookingmanagement/{bookingId}/check-in`

**Authorization:** Required (Roles: Receptionist, Manager, Admin)

**Description:** Check-in booking - Chuyển trạng thái booking sang CheckedIn và cập nhật room status sang Occupied. API này được sử dụng khi khách hàng đến khách sạn để nhận phòng.

---

## Request

### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bookingId` | integer | ✅ Yes | ID của booking cần check-in |

### Headers

```http
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Request Body

**Không cần body** - Chỉ cần `bookingId` trong URL path.

### Example Request

```http
POST /api/bookingmanagement/123/check-in
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```bash
# cURL Example
curl -X POST "https://api.hotel.com/api/bookingmanagement/123/check-in" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

```javascript
// JavaScript/Fetch Example
const bookingId = 123;

const response = await fetch(`/api/bookingmanagement/${bookingId}/check-in`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});

const result = await response.json();
console.log(result);
```

```typescript
// TypeScript/Axios Example
import axios from 'axios';

const checkInBooking = async (bookingId: number, token: string) => {
  try {
    const response = await axios.post(
      `/api/bookingmanagement/${bookingId}/check-in`,
      {},
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );
    return response.data;
  } catch (error) {
    console.error('Check-in failed:', error.response?.data);
    throw error;
  }
};

// Usage
const result = await checkInBooking(123, userToken);
```

---

## Response

### Success Response (200 OK)

**Status Code:** `200 OK`

**Response Body:**

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Check-in thành công. Chúc quý khách có kỳ nghỉ vui vẻ!",
  "data": {
    "bookingId": 123,
    "checkInTime": "2024-12-16T14:30:00Z",
    "roomNumbers": ["101", "102"],
    "customerName": "Nguyễn Văn A",
    "checkOutDate": "2024-12-20T12:00:00Z"
  }
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `isSuccess` | boolean | Trạng thái thành công của request |
| `statusCode` | integer | HTTP status code |
| `message` | string | Thông báo kết quả |
| `data` | object | Thông tin check-in |
| `data.bookingId` | integer | ID của booking đã check-in |
| `data.checkInTime` | string (ISO 8601) | Thời gian check-in thực tế |
| `data.roomNumbers` | string[] | Danh sách số phòng đã check-in |
| `data.customerName` | string | Tên khách hàng |
| `data.checkOutDate` | string (ISO 8601) | Ngày dự kiến check-out |

---

### Error Responses

#### 1. Booking Not Found (404)

```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy booking"
}
```

#### 2. Invalid Booking Status (400)

**Trường hợp: Booking không ở trạng thái Confirmed hoặc Pending**

```json
{
  "isSuccess": false,
  "statusCode": 400,
  "message": "Không thể check-in. Booking đang ở trạng thái: Đã hoàn thành"
}
```

**Các trạng thái không cho phép check-in:**
- `Cancelled` - Đã hủy
- `Completed` - Đã hoàn thành
- `CheckedIn` - Đã check-in rồi
- `PendingConfirmation` - Chờ xác nhận thanh toán

**Chỉ cho phép check-in khi:**
- Status = `Confirmed` (Đã xác nhận)
- Status = `Pending` (Chờ thanh toán - cho offline booking)

#### 3. No Booking Rooms Found (404)

```json
{
  "isSuccess": false,
  "statusCode": 404,
  "message": "Không tìm thấy thông tin phòng của booking"
}
```

#### 4. Unauthorized (401)

```json
{
  "isSuccess": false,
  "statusCode": 401,
  "message": "Unauthorized. Token không hợp lệ hoặc đã hết hạn."
}
```

#### 5. Forbidden (403)

```json
{
  "isSuccess": false,
  "statusCode": 403,
  "message": "Access denied. Bạn không có quyền check-in booking."
}
```

**Note:** Chỉ có Receptionist, Manager, Admin mới có quyền check-in.

#### 6. System Error (500)

```json
{
  "isSuccess": false,
  "statusCode": 500,
  "message": "Lỗi: Không tìm thấy RoomStatus 'Occupied' trong hệ thống"
}
```

hoặc

```json
{
  "isSuccess": false,
  "statusCode": 500,
  "message": "Lỗi: Database connection failed"
}
```

---

## Business Logic

### Pre-conditions (Điều kiện trước khi check-in)

1. ✅ Booking phải tồn tại trong hệ thống
2. ✅ Booking status phải là `Confirmed` hoặc `Pending`
3. ✅ Booking phải có ít nhất 1 phòng đã được đặt
4. ✅ User phải có role Receptionist/Manager/Admin

### What Happens During Check-In?

1. **Validate Booking Status**
   - Kiểm tra booking tồn tại
   - Kiểm tra status hợp lệ (Confirmed hoặc Pending)

2. **Update Room Status**
   - Tất cả phòng trong booking → Status = `Occupied` (Đang sử dụng)
   - Cập nhật `UpdatedAt` timestamp

3. **Update Booking Status**
   - Booking status → `CheckedIn`
   - Cập nhật `UpdatedBy` = employeeId (người check-in)
   - Cập nhật `UpdatedAt` timestamp

4. **Log Activity**
   - Ghi log check-in activity
   - Lưu thông tin nhân viên thực hiện check-in

5. **(Optional) Send Welcome Email**
   - Gửi email chào mừng cho khách hàng
   - Email không bắt buộc - nếu fail không ảnh hưởng check-in

### State Transitions

```
Before Check-In:
- Booking Status: Confirmed/Pending
- Room Status: Available

After Check-In:
- Booking Status: CheckedIn
- Room Status: Occupied
```

---

## Use Cases

### Use Case 1: Online Booking - Standard Check-In

**Scenario:** Khách đặt online, đã thanh toán deposit 30%, đến khách sạn check-in đúng giờ.

**Flow:**
1. Receptionist tìm booking theo tên/email/SĐT
2. Click "Check-In" trên booking có status `Confirmed`
3. System validate và cập nhật status
4. Trao chìa khóa phòng cho khách

**Request:**
```bash
POST /api/bookingmanagement/456/check-in
```

**Response:**
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Check-in thành công. Chúc quý khách có kỳ nghỉ vui vẻ!",
  "data": {
    "bookingId": 456,
    "checkInTime": "2024-12-16T14:00:00Z",
    "roomNumbers": ["305"],
    "customerName": "Trần Thị B",
    "checkOutDate": "2024-12-18T12:00:00Z"
  }
}
```

### Use Case 2: Walk-In Booking - Immediate Check-In

**Scenario:** Khách walk-in tại quầy, đặt phòng và check-in ngay.

**Flow:**
1. Receptionist tạo booking offline (status = `CheckedIn` luôn)
2. Không cần gọi API check-in vì đã check-in khi tạo booking

**Note:** Offline booking tự động ở status `CheckedIn`, không cần check-in thủ công.

### Use Case 3: Multiple Rooms Check-In

**Scenario:** Booking có nhiều phòng (family/group), check-in cùng lúc.

**Request:**
```bash
POST /api/bookingmanagement/789/check-in
```

**Response:**
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "message": "Check-in thành công. Chúc quý khách có kỳ nghỉ vui vẻ!",
  "data": {
    "bookingId": 789,
    "checkInTime": "2024-12-16T15:30:00Z",
    "roomNumbers": ["201", "202", "203"],
    "customerName": "Lê Văn C",
    "checkOutDate": "2024-12-20T12:00:00Z"
  }
}
```

---

## Frontend Integration Guide

### React/TypeScript Example

```typescript
// types.ts
export interface CheckInResponse {
  isSuccess: boolean;
  statusCode: number;
  message: string;
  data?: {
    bookingId: number;
    checkInTime: string;
    roomNumbers: string[];
    customerName: string;
    checkOutDate: string;
  };
}

// api.ts
import axios from 'axios';

export const checkInBooking = async (
  bookingId: number,
  token: string
): Promise<CheckInResponse> => {
  const response = await axios.post(
    `/api/bookingmanagement/${bookingId}/check-in`,
    {},
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  return response.data;
};

// CheckInButton.tsx
import React, { useState } from 'react';
import { checkInBooking } from './api';
import { Button, message } from 'antd';

interface Props {
  bookingId: number;
  onSuccess: () => void;
}

export const CheckInButton: React.FC<Props> = ({ bookingId, onSuccess }) => {
  const [loading, setLoading] = useState(false);

  const handleCheckIn = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      if (!token) {
        message.error('Vui lòng đăng nhập');
        return;
      }

      const result = await checkInBooking(bookingId, token);
      
      if (result.isSuccess) {
        message.success(result.message);
        message.info(`Phòng: ${result.data?.roomNumbers.join(', ')}`);
        onSuccess();
      } else {
        message.error(result.message);
      }
    } catch (error: any) {
      const errorMsg = error.response?.data?.message || 'Check-in thất bại';
      message.error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Button 
      type="primary" 
      onClick={handleCheckIn}
      loading={loading}
    >
      Check-In
    </Button>
  );
};
```

### Vue 3 Example

```vue
<template>
  <button 
    @click="handleCheckIn" 
    :disabled="loading"
    class="btn-checkin"
  >
    {{ loading ? 'Đang xử lý...' : 'Check-In' }}
  </button>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import axios from 'axios';
import { ElMessage } from 'element-plus';

interface Props {
  bookingId: number;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: 'success'): void;
}>();

const loading = ref(false);

const handleCheckIn = async () => {
  loading.value = true;
  
  try {
    const token = localStorage.getItem('token');
    const response = await axios.post(
      `/api/bookingmanagement/${props.bookingId}/check-in`,
      {},
      {
        headers: { 'Authorization': `Bearer ${token}` }
      }
    );

    if (response.data.isSuccess) {
      ElMessage.success(response.data.message);
      emit('success');
    } else {
      ElMessage.error(response.data.message);
    }
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || 'Check-in thất bại');
  } finally {
    loading.value = false;
  }
};
</script>
```

---

## Testing Guide

### Manual Testing Steps

1. **Setup Test Data**
   - Tạo 1 booking với status `Confirmed`
   - Đảm bảo booking có ít nhất 1 phòng

2. **Test Success Case**
   ```bash
   POST /api/bookingmanagement/1/check-in
   Authorization: Bearer {receptionist_token}
   
   Expected: 200 OK, booking status → CheckedIn
   ```

3. **Test Error Cases**
   ```bash
   # Case 1: Booking không tồn tại
   POST /api/bookingmanagement/99999/check-in
   Expected: 404 Not Found
   
   # Case 2: Booking đã check-in rồi
   POST /api/bookingmanagement/1/check-in (lần 2)
   Expected: 400 Bad Request
   
   # Case 3: Không có quyền
   POST /api/bookingmanagement/1/check-in
   Authorization: Bearer {customer_token}
   Expected: 403 Forbidden
   ```

### Postman Collection

```json
{
  "info": {
    "name": "Check-In Booking API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Check-In Success",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{receptionist_token}}",
            "type": "text"
          }
        ],
        "url": {
          "raw": "{{base_url}}/api/bookingmanagement/{{bookingId}}/check-in",
          "host": ["{{base_url}}"],
          "path": ["api", "bookingmanagement", "{{bookingId}}", "check-in"]
        }
      }
    },
    {
      "name": "Check-In - Booking Not Found",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{receptionist_token}}",
            "type": "text"
          }
        ],
        "url": {
          "raw": "{{base_url}}/api/bookingmanagement/99999/check-in",
          "host": ["{{base_url}}"],
          "path": ["api", "bookingmanagement", "99999", "check-in"]
        }
      }
    }
  ]
}
```

---

## Notes & Best Practices

### ⚠️ Important Notes

1. **Check-In Time:** API không validate thời gian check-in (có thể check-in sớm/muộn). Frontend nên hiển thị warning nếu check-in sớm hơn `CheckInDate`.

2. **Room Assignment:** API không kiểm tra xem phòng có đang bị occupied bởi booking khác không. Hệ thống giả định phòng đã được validate khi tạo booking.

3. **Payment Status:** Check-in không yêu cầu thanh toán đầy đủ. Khách có thể check-in với chỉ deposit (30%) và trả phần còn lại khi checkout.

4. **Idempotency:** API **KHÔNG** idempotent. Gọi 2 lần sẽ trả về lỗi lần thứ 2 (booking đã CheckedIn).

### ✅ Best Practices

1. **Show Confirmation Dialog** trước khi check-in
2. **Validate date** - warning nếu check-in sớm/muộn
3. **Display room numbers** sau khi check-in thành công
4. **Refresh booking list** sau khi check-in
5. **Error handling** - hiển thị message rõ ràng cho từng error case
6. **Loading state** - disable button khi đang xử lý
7. **Permission check** - chỉ show button cho Receptionist/Manager/Admin

---

## Related APIs

- `POST /api/bookingmanagement/offline` - Tạo booking offline (auto check-in)
- `POST /api/bookingmanagement/{bookingId}/confirm` - Xác nhận booking online
- `POST /api/checkout/{bookingId}` - Checkout booking
- `GET /api/bookingmanagement/{bookingId}` - Lấy chi tiết booking

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2024-12-16 | Initial API documentation |


