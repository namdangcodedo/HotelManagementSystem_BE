# Dashboard API Documentation

## 📚 Overview

Tài liệu này mô tả chi tiết các API endpoints của Dashboard module trong hệ thống quản lý khách sạn.

**Base URL:** `/api/Dashboard`

**Authentication:** Tất cả endpoints yêu cầu Bearer Token và Role: `Manager` hoặc `Admin`

---

## 📊 API Endpoints

### 1. GET /api/Dashboard/stats

**Priority:** ✅ **CAO NHẤT** - Required

**Description:** Lấy toàn bộ thống kê dashboard trong một lần gọi API

**Use Case:** API chính cho màn hình Dashboard admin. Frontend sẽ gọi endpoint này mỗi 60 giây để refresh data.

#### Request

```http
GET /api/Dashboard/stats HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
```

**Query Parameters:** Không có

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
    "totalBookings": 95,
    "bookingsThisMonth": 22,
    "bookingsLastMonth": 19,
    "bookingsGrowth": 15.8,
    "totalRevenue": 12500000,
    "revenueThisMonth": 4800000,
    "revenueLastMonth": 4300000,
    "revenueGrowth": 11.6,
    "averageRoomRate": 850000,
    "totalCustomers": 28,
    "newCustomersThisMonth": 4,
    "customersGrowth": 6.5,
    "totalRooms": 30,
    "availableRooms": 10,
    "occupiedRooms": 17,
    "maintenanceRooms": 3,
    "occupancyRate": 56.7,
    "totalTransactions": 100,
    "completedPayments": 93,
    "pendingPayments": 7
  },
  "success": true,
  "message": "Get statistics successfully",
  "responseCode": "SUCCESS",
  "statusCode": 200
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | object | Statistics data object |
| `data.totalBookings` | integer | Tổng số booking (all time) |
| `data.bookingsThisMonth` | integer | Số booking tháng hiện tại |
| `data.bookingsLastMonth` | integer | Số booking tháng trước |
| `data.bookingsGrowth` | decimal | % tăng trưởng booking so với tháng trước |
| `data.totalRevenue` | decimal | Tổng doanh thu (VNĐ) (all time) |
| `data.revenueThisMonth` | decimal | Doanh thu tháng hiện tại (VNĐ) |
| `data.revenueLastMonth` | decimal | Doanh thu tháng trước (VNĐ) |
| `data.revenueGrowth` | decimal | % tăng trưởng doanh thu so với tháng trước |
| `data.averageRoomRate` | decimal | Giá trung bình mỗi đêm (VNĐ) |
| `data.totalCustomers` | integer | Tổng số khách hàng |
| `data.newCustomersThisMonth` | integer | Số khách mới tháng này |
| `data.customersGrowth` | decimal | % tăng trưởng khách hàng |
| `data.totalRooms` | integer | Tổng số phòng |
| `data.availableRooms` | integer | Số phòng trống |
| `data.occupiedRooms` | integer | Số phòng đang sử dụng |
| `data.maintenanceRooms` | integer | Số phòng bảo trì |
| `data.occupancyRate` | decimal | Tỷ lệ lấp phòng (%) |
| `data.totalTransactions` | integer | Tổng số giao dịch |
| `data.completedPayments` | integer | Số giao dịch đã thanh toán |
| `data.pendingPayments` | integer | Số giao dịch chờ thanh toán |
| `success` | boolean | Trạng thái thành công |
| `message` | string | Thông báo |
| `responseCode` | string | Mã response |
| `statusCode` | integer | HTTP status code |

**Error Response (401 Unauthorized):**

```json
{
  "success": false,
  "message": "Unauthorized access",
  "responseCode": "UNAUTHORIZED",
  "statusCode": 401
}
```

**Error Response (403 Forbidden):**

```json
{
  "success": false,
  "message": "Access denied. Require Manager or Admin role",
  "responseCode": "FORBIDDEN",
  "statusCode": 403
}
```

**Error Response (500 Internal Server Error):**

```json
{
  "success": false,
  "message": "Error retrieving dashboard statistics: {error_details}",
  "responseCode": "SERVER_ERROR",
  "statusCode": 500
}
```

#### Business Logic

- **Booking Growth:** `((bookingsThisMonth - bookingsLastMonth) / bookingsLastMonth) * 100`
- **Revenue Growth:** `((revenueThisMonth - revenueLastMonth) / revenueLastMonth) * 100`
- **Customer Growth:** `((newCustomersThisMonth - newCustomersLastMonth) / newCustomersLastMonth) * 100`
- **Occupancy Rate:** `(occupiedRooms / totalRooms) * 100`
- **Average Room Rate:** `totalRevenue / totalNights` (calculated from completed bookings)

#### Notes

- Dữ liệu "this month" được tính từ ngày 1 của tháng hiện tại đến hiện tại
- Dữ liệu "last month" được tính toàn bộ tháng trước
- Occupied rooms: Phòng có booking active (CheckInDate <= now AND CheckOutDate > now)
- Maintenance rooms: Phòng có StatusId = "Maintenance"
- Available rooms: totalRooms - occupiedRooms - maintenanceRooms

#### Example Usage

**cURL:**
```bash
curl -X GET "https://your-api-host.com/api/Dashboard/stats" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

**JavaScript (Fetch):**
```javascript
const response = await fetch('https://your-api-host.com/api/Dashboard/stats', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json'
  }
});
const data = await response.json();
```

**TypeScript (React Query):**
```typescript
const { data, isLoading } = useQuery({
  queryKey: ['dashboard-stats'],
  queryFn: async () => {
    const response = await fetch('/api/Dashboard/stats', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    return response.json();
  },
  refetchInterval: 60000 // Refetch every 60 seconds
});
```

---

### 2. GET /api/Dashboard/room-status

**Priority:** ⚠️ **TRUNG BÌNH** - Optional

**Description:** Lấy chi tiết phân bố trạng thái phòng (available, occupied, maintenance)

**Use Case:** Hiển thị chi tiết trạng thái phòng. Data này có thể tính từ `/stats` endpoint nên không bắt buộc.

#### Request

```http
GET /api/Dashboard/room-status HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
```

**Query Parameters:** Không có

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Response

**Success Response (200 OK):**

```json
{
  "data": [
    {
      "status": "available",
      "count": 10,
      "percentage": 33.3
    },
    {
      "status": "occupied",
      "count": 17,
      "percentage": 56.7
    },
    {
      "status": "maintenance",
      "count": 3,
      "percentage": 10.0
    }
  ],
  "success": true,
  "message": "Get room status successfully",
  "responseCode": "SUCCESS",
  "statusCode": 200
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | array | Mảng các object trạng thái phòng |
| `data[].status` | string | Trạng thái: "available", "occupied", "maintenance" |
| `data[].count` | integer | Số lượng phòng |
| `data[].percentage` | decimal | Phần trăm (%) |

**Error Response:** Tương tự endpoint `/stats`

#### Business Logic

- **Percentage:** `(count / totalRooms) * 100`
- Occupied rooms: Phòng có booking đang hoạt động
- Available rooms: Phòng không bị chiếm và không bảo trì
- Maintenance rooms: Phòng có trạng thái bảo trì

#### Example Usage

**cURL:**
```bash
curl -X GET "https://your-api-host.com/api/Dashboard/room-status" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**TypeScript:**
```typescript
interface RoomStatus {
  status: 'available' | 'occupied' | 'maintenance';
  count: number;
  percentage: number;
}

const { data } = await fetch('/api/Dashboard/room-status', {
  headers: { 'Authorization': `Bearer ${token}` }
}).then(res => res.json());
```

---

### 3. GET /api/Dashboard/revenue-by-month

**Priority:** 📊 **THẤP** - For future features

**Description:** Lấy dữ liệu doanh thu theo từng tháng để vẽ biểu đồ

**Use Case:** Biểu đồ doanh thu theo tháng (tính năng tương lai)

#### Request

```http
GET /api/Dashboard/revenue-by-month?months=12 HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
```

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `months` | integer | No | 12 | Số tháng muốn lấy (1-24) |

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Response

**Success Response (200 OK):**

```json
{
  "data": [
    {
      "month": "12",
      "year": 2023,
      "revenue": 3500000,
      "bookings": 18
    },
    {
      "month": "01",
      "year": 2024,
      "revenue": 4800000,
      "bookings": 22
    },
    {
      "month": "02",
      "year": 2024,
      "revenue": 5200000,
      "bookings": 25
    }
  ],
  "success": true,
  "message": "Get revenue by month successfully",
  "responseCode": "SUCCESS",
  "statusCode": 200
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | array | Mảng dữ liệu doanh thu theo tháng |
| `data[].month` | string | Tháng (format: "01", "02", ..., "12") |
| `data[].year` | integer | Năm |
| `data[].revenue` | decimal | Doanh thu tháng đó (VNĐ) |
| `data[].bookings` | integer | Số booking trong tháng |

**Error Response:** Tương tự endpoint `/stats`

#### Business Logic

- Chỉ tính doanh thu từ transactions có PaymentStatus = "Paid"
- Dữ liệu được sắp xếp theo thứ tự thời gian (oldest first)
- Nếu tháng không có doanh thu, tháng đó sẽ không xuất hiện trong response

#### Example Usage

**cURL:**
```bash
curl -X GET "https://your-api-host.com/api/Dashboard/revenue-by-month?months=6" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**TypeScript:**
```typescript
interface RevenueByMonth {
  month: string;
  year: number;
  revenue: number;
  bookings: number;
}

const response = await fetch('/api/Dashboard/revenue-by-month?months=12', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const { data }: { data: RevenueByMonth[] } = await response.json();
```

---

### 4. GET /api/Dashboard/top-room-types

**Priority:** 📊 **THẤP** - For future features

**Description:** Lấy danh sách các loại phòng có doanh thu/booking cao nhất

**Use Case:** Thống kê loại phòng phổ biến (tính năng tương lai)

#### Request

```http
GET /api/Dashboard/top-room-types?limit=5 HTTP/1.1
Host: your-api-host.com
Authorization: Bearer {access_token}
```

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `limit` | integer | No | 5 | Số lượng room types muốn lấy |

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

#### Response

**Success Response (200 OK):**

```json
{
  "data": [
    {
      "roomTypeId": 1,
      "typeName": "Deluxe",
      "bookingCount": 45,
      "totalRevenue": 6800000,
      "averagePrice": 850000,
      "availableRooms": 0,
      "popularityScore": 0
    },
    {
      "roomTypeId": 2,
      "typeName": "Suite",
      "bookingCount": 28,
      "totalRevenue": 5600000,
      "averagePrice": 1200000,
      "availableRooms": 0,
      "popularityScore": 0
    }
  ],
  "success": true,
  "message": "Get top room types successfully",
  "responseCode": "SUCCESS",
  "statusCode": 200
}
```

**Response Schema:**

| Field | Type | Description |
|-------|------|-------------|
| `data` | array | Mảng các loại phòng top |
| `data[].roomTypeId` | integer | ID của loại phòng |
| `data[].typeName` | string | Tên loại phòng |
| `data[].bookingCount` | integer | Số lần booking |
| `data[].totalRevenue` | decimal | Tổng doanh thu (VNĐ) |
| `data[].averagePrice` | decimal | Giá trung bình mỗi booking (VNĐ) |
| `data[].availableRooms` | integer | Số phòng còn trống (reserved for future) |
| `data[].popularityScore` | decimal | Điểm phổ biến (reserved for future) |

**Error Response:** Tương tự endpoint `/stats`

#### Business Logic

- Sắp xếp theo `totalRevenue` giảm dần (DESC)
- `averagePrice = totalRevenue / bookingCount`
- Chỉ tính bookings trong khoảng thời gian được chỉ định (mặc định: 1 tháng trở lại)

#### Example Usage

**cURL:**
```bash
curl -X GET "https://your-api-host.com/api/Dashboard/top-room-types?limit=10" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**TypeScript:**
```typescript
interface TopRoomType {
  roomTypeId: number;
  typeName: string;
  bookingCount: number;
  totalRevenue: number;
  averagePrice: number;
}

const response = await fetch('/api/Dashboard/top-room-types?limit=5', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const { data }: { data: TopRoomType[] } = await response.json();
```

---

## 🔒 Authentication & Authorization

### Authentication

Tất cả endpoints yêu cầu JWT Bearer Token trong header:

```
Authorization: Bearer {access_token}
```

### Authorization

Chỉ users với roles sau mới có quyền truy cập:
- **Manager**
- **Admin**

### Common Error Responses

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "Unauthorized access",
  "responseCode": "UNAUTHORIZED",
  "statusCode": 401
}
```

**403 Forbidden:**
```json
{
  "success": false,
  "message": "Access denied. Require Manager or Admin role",
  "responseCode": "FORBIDDEN",
  "statusCode": 403
}
```

---

## 📊 Response Format

Tất cả API responses đều follow cấu trúc chung:

```typescript
interface ApiResponse<T> {
  data: T;
  success: boolean;
  message: string;
  responseCode: string;
  statusCode: number;
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
| `SERVER_ERROR` | Lỗi server |

---

## 🚀 Best Practices

### Caching

Frontend nên implement caching strategy:
- `/stats` endpoint: Refetch mỗi **60 giây**
- `/room-status` endpoint: Refetch mỗi **30 giây**
- `/revenue-by-month` và `/top-room-types`: Cache longer hoặc on-demand

### Error Handling

```typescript
try {
  const response = await fetch('/api/Dashboard/stats', {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  if (!response.ok) {
    if (response.status === 401) {
      // Redirect to login
      redirectToLogin();
    } else if (response.status === 403) {
      // Show access denied message
      showAccessDenied();
    } else {
      // Show generic error
      showError('Failed to load dashboard data');
    }
    return;
  }

  const data = await response.json();
  if (data.success) {
    // Handle success
    updateDashboard(data.data);
  }
} catch (error) {
  // Handle network error
  console.error('Network error:', error);
}
```

### TypeScript Types

```typescript
// Dashboard Stats
interface DashboardStats {
  totalBookings: number;
  bookingsThisMonth: number;
  bookingsLastMonth: number;
  bookingsGrowth: number;
  totalRevenue: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  revenueGrowth: number;
  averageRoomRate: number;
  totalCustomers: number;
  newCustomersThisMonth: number;
  customersGrowth: number;
  totalRooms: number;
  availableRooms: number;
  occupiedRooms: number;
  maintenanceRooms: number;
  occupancyRate: number;
  totalTransactions: number;
  completedPayments: number;
  pendingPayments: number;
}

// Room Status
interface RoomStatus {
  status: 'available' | 'occupied' | 'maintenance';
  count: number;
  percentage: number;
}

// Revenue By Month
interface RevenueByMonth {
  month: string;
  year: number;
  revenue: number;
  bookings: number;
}

// Top Room Type
interface TopRoomType {
  roomTypeId: number;
  typeName: string;
  bookingCount: number;
  totalRevenue: number;
  averagePrice: number;
  availableRooms: number;
  popularityScore: number;
}
```

---

## 📞 Support

Nếu có thắc mắc về API, vui lòng liên hệ:
- Backend Team
- Email: support@hotel-management.com
- Documentation: [Link to main API docs]

---

## 📝 Change Log

### Version 1.0.0 (2024-01-XX)
- ✅ Initial release
- ✅ Implemented `/stats` endpoint (Priority: HIGH)
- ✅ Implemented `/room-status` endpoint (Priority: MEDIUM)
- ✅ Implemented `/revenue-by-month` endpoint (Priority: LOW)
- ✅ Implemented `/top-room-types` endpoint (Priority: LOW)

---

**Last Updated:** 2024-01-XX
**API Version:** 1.0.0
**Backend:** ASP.NET Core 9.0
