# Customer Management API

Các endpoint để Admin/Manager/Receptionist xem thông tin khách hàng, thống kê booking và khoá/mở khoá tài khoản. Base path: `/api/customers`.

## 1) Danh sách khách hàng
**Endpoint:** `GET /api/customers`  
**Roles:** Admin, Manager, Receptionist

**Query params**
- `pageIndex` (int, default 1)
- `pageSize` (int, default 10)
- `search` (string, optional) - tìm theo tên/điện thoại/CMND/Email
- `isLocked` (bool, optional) - lọc theo trạng thái khoá tài khoản
- `fromDate`, `toDate` (ISO date, optional) - lọc theo ngày tạo
- `sortBy` (string, optional) - ví dụ: `CreatedAt`, `FullName`
- `sortDesc` (bool, default false)

**Response mẫu**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Retrieved successfully.",
  "data": {
    "items": [
      {
        "customerId": 12,
        "accountId": 25,
        "fullName": "Nguyen Van A",
        "email": "user@example.com",
        "phoneNumber": "0901234567",
        "isLocked": false,
        "totalBookings": 4,
        "totalSpent": 15200000,
        "lastBookingDate": "2024-12-10T09:30:00Z",
        "createdAt": "2024-10-02T04:15:20Z"
      }
    ],
    "totalCount": 1,
    "pageIndex": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "statusCode": 200
}
```

## 2) Chi tiết khách hàng
**Endpoint:** `GET /api/customers/{customerId}`  
**Roles:** Admin, Manager, Receptionist  
**Mô tả:** Trả về thông tin hồ sơ, tài khoản, tổng số booking, số booking hoàn thành/hủy, tổng tiền đã chi, số giao dịch, số feedback, và 5 booking gần nhất.

**Response mẫu**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Retrieved successfully.",
  "data": {
    "basicInfo": {
      "customerId": 12,
      "accountId": 25,
      "fullName": "Nguyen Van A",
      "email": "user@example.com",
      "phoneNumber": "0901234567",
      "identityCard": "0123456789",
      "address": "1 Vo Van Tan, Q3",
      "avatarUrl": "https://res.cloudinary.com/.../avatar.png",
      "createdAt": "2024-10-02T04:15:20Z"
    },
    "account": {
      "accountId": 25,
      "username": "usera",
      "email": "user@example.com",
      "isLocked": false,
      "lastLoginAt": "2024-12-10T02:15:00Z",
      "roles": ["User"]
    },
    "statistics": {
      "totalBookings": 4,
      "completedBookings": 3,
      "cancelledBookings": 1,
      "upcomingBookings": 1,
      "totalSpent": 15200000,
      "lastBookingDate": "2024-12-10T09:30:00Z",
      "totalFeedbacks": 2,
      "totalTransactions": 4,
      "totalPaidAmount": 15000000
    },
    "recentBookings": [
      {
        "bookingId": 101,
        "statusCode": "Completed",
        "statusName": "Hoàn thành",
        "bookingType": "Online",
        "checkInDate": "2024-12-12T00:00:00Z",
        "checkOutDate": "2024-12-14T00:00:00Z",
        "totalAmount": 5200000,
        "createdAt": "2024-12-01T08:00:00Z"
      }
    ]
  },
  "statusCode": 200
}
```

## 3) Khoá/Mở khoá khách hàng
**Endpoint:** `PATCH /api/customers/{customerId}/ban`  
**Roles:** Admin  
**Body**
```json
{ "isLocked": true }
```

**Response mẫu**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "message": "Khoá tài khoản khách hàng thành công",
  "data": {
    "customerId": 12,
    "isLocked": true
  },
  "statusCode": 200
}
```

Ghi chú:
- Thời gian trả về ở dạng UTC.
- `recentBookings` tối đa 5 bản ghi, phục vụ FE hiển thị lịch sử tham gia nhanh.
