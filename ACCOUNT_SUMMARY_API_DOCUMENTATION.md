# API Documentation: Account Summary

## 📋 Tổng quan

API Account Summary cung cấp thông tin tổng quan về một tài khoản, bao gồm:
- Thông tin cơ bản của Account (username, email, roles, trạng thái)
- Phân loại tài khoản (Customer hoặc Employee)
- Thông tin chi tiết profile tương ứng
- **Statistics** (chỉ hiển thị khi Admin xem)

## 🔐 Phân quyền

### Endpoint 1: `GET /api/Account/summary`
**Mô tả:** Xem summary của chính mình hoặc người khác (Admin only)

**Query Parameters:**
- `accountId` (optional): ID tài khoản muốn xem. Nếu không truyền, mặc định xem của chính mình.

**Authorization:**
- ✅ **Mọi user đã login** có thể xem summary của **chính mình**
- ✅ **Admin** có thể xem summary của **bất kỳ ai** (kèm statistics)
- ❌ **Non-Admin** KHÔNG thể xem summary của người khác → 403 Forbidden

### Endpoint 2: `GET /api/Account/summary/{id}`
**Mô tả:** Xem summary của một tài khoản cụ thể

**Route Parameters:**
- `id` (required): ID tài khoản cần xem

**Authorization:**
- ✅ **Chỉ Admin** được sử dụng endpoint này
- ❌ Manager, Employee, Customer → 403 Forbidden

## 📊 Response Structure

### Base Response (Tất cả users)
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Retrieved successfully.",
  "data": {
    "accountId": 1,
    "username": "admin",
    "email": "admin@hotel.com",
    "isLocked": false,
    "lastLoginAt": "2024-10-15T10:30:00Z",
    "createdAt": "2024-01-01T00:00:00Z",
    "roles": ["Admin"],
    "accountType": "Employee", // hoặc "Customer"
    "profileDetails": { ... },
    "statistics": null // hoặc { ... } nếu Admin xem
  }
}
```

### Customer Profile Details
```json
"profileDetails": {
  "customerId": 1,
  "fullName": "Nguyễn Văn Khách",
  "phoneNumber": "0911111111",
  "identityCard": "123456789",
  "address": "123 Đường ABC, TP.HCM",
  "avatarUrl": "https://cloudinary.com/avatar.jpg"
}
```

### Employee Profile Details
```json
"profileDetails": {
  "employeeId": 2,
  "fullName": "Nguyễn Văn Quản Lý",
  "phoneNumber": "0900000002",
  "employeeTypeId": 13,
  "employeeTypeName": "Quản lý",
  "hireDate": "2024-04-15",
  "terminationDate": null,
  "isActive": true
}
```

### Customer Statistics (Admin only)
```json
"statistics": {
  "totalBookings": 5,           // Tổng số booking
  "completedBookings": 3,       // Booking đã hoàn thành
  "cancelledBookings": 1,       // Booking đã hủy
  "totalSpent": 5000000,        // Tổng chi tiêu (VNĐ)
  "totalFeedbacks": 2,          // Số lượng feedback đã gửi
  "totalNotifications": 10,     // Tổng thông báo
  "unreadNotifications": 3      // Thông báo chưa đọc
}
```

### Employee Statistics (Admin only)
```json
"statistics": {
  "totalTasksAssigned": 20,     // Tổng công việc được giao
  "completedTasks": 18,         // Công việc đã hoàn thành
  "pendingTasks": 2,            // Công việc đang chờ
  "totalAttendance": 120,       // Tổng số ngày điểm danh
  "totalSalaryPaid": 60000000,  // Tổng lương đã nhận (VNĐ)
  "workingDays": 183,           // Số ngày làm việc
  "totalNotifications": 15,     // Tổng thông báo
  "unreadNotifications": 5      // Thông báo chưa đọc
}
```

## 🎯 Use Cases

### Use Case 1: User xem profile của chính mình
**Request:**
```http
GET /api/Account/summary
Authorization: Bearer {user_token}
```

**Response:**
- Thông tin account cơ bản
- Profile details (Customer hoặc Employee)
- **KHÔNG** có statistics

### Use Case 2: Admin xem profile của Customer
**Request:**
```http
GET /api/Account/summary?accountId=4
Authorization: Bearer {admin_token}
```

hoặc

```http
GET /api/Account/summary/4
Authorization: Bearer {admin_token}
```

**Response:**
- Thông tin account đầy đủ
- Customer profile details
- **CÓ** statistics về bookings, chi tiêu, feedbacks

### Use Case 3: Admin xem profile của Employee
**Request:**
```http
GET /api/Account/summary/2
Authorization: Bearer {admin_token}
```

**Response:**
- Thông tin account đầy đủ
- Employee profile details
- **CÓ** statistics về tasks, attendance, salary

### Use Case 4: Manager cố xem profile của Admin (Forbidden)
**Request:**
```http
GET /api/Account/summary?accountId=1
Authorization: Bearer {manager_token}
```

**Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```

## 📝 Business Logic

### Xác định Account Type
1. Kiểm tra `account.Customer != null` → AccountType = "Customer"
2. Kiểm tra `account.Employee != null` → AccountType = "Employee"
3. Nếu cả hai đều null → Tài khoản không hoàn chỉnh

### Lấy Statistics
**Điều kiện:**
- `requesterId` phải được truyền vào
- Requester phải có role "Admin"

**Customer Statistics:**
- Query từ `Bookings` table
- Tính tổng `TotalPrice`
- Đếm `Feedbacks`
- Đếm `Notifications`

**Employee Statistics:**
- Query từ `HousekeepingTasks` table
- Query từ `Attendances` table
- Tính tổng từ `Salaries` table
- Tính số ngày làm việc từ `HireDate`

## 🔍 Error Handling

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```
**Nguyên nhân:** Không có token hoặc token không hợp lệ

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.4",
  "title": "Forbidden",
  "status": 403
}
```
**Nguyên nhân:**
- Non-Admin cố xem summary của người khác
- Non-Admin cố dùng endpoint `/api/Account/summary/{id}`

### 404 Not Found
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "message": "Tài khoản not found."
}
```
**Nguyên nhân:** Account ID không tồn tại

## 🧪 Testing Scenarios

### Scenario 1: Happy Path - Customer tự xem
```bash
GET /api/Account/summary
Authorization: Bearer {customer_token}

Expected: 200 OK với profileDetails là Customer, statistics = null
```

### Scenario 2: Happy Path - Admin xem Customer
```bash
GET /api/Account/summary/4
Authorization: Bearer {admin_token}

Expected: 200 OK với Customer statistics đầy đủ
```

### Scenario 3: Happy Path - Admin xem Employee
```bash
GET /api/Account/summary/2
Authorization: Bearer {admin_token}

Expected: 200 OK với Employee statistics đầy đủ
```

### Scenario 4: Forbidden - Manager xem Admin
```bash
GET /api/Account/summary?accountId=1
Authorization: Bearer {manager_token}

Expected: 403 Forbidden
```

### Scenario 5: Forbidden - Customer xem người khác
```bash
GET /api/Account/summary?accountId=2
Authorization: Bearer {customer_token}

Expected: 403 Forbidden
```

### Scenario 6: Not Found
```bash
GET /api/Account/summary/99999
Authorization: Bearer {admin_token}

Expected: 404 Not Found
```

## 💡 Best Practices

1. **Security:**
   - Luôn verify role trước khi cho phép xem statistics
   - Check ownership trước khi trả về data
   - Không expose sensitive data trong error messages

2. **Performance:**
   - Statistics chỉ được tính khi cần thiết (Admin view)
   - Sử dụng eager loading để giảm số lượng queries
   - Cache statistics nếu có thể

3. **Frontend Integration:**
   ```javascript
   // User xem profile của chính mình
   GET /api/Account/summary
   
   // Admin dashboard - xem profile user cụ thể
   GET /api/Account/summary/{userId}
   ```

4. **Data Privacy:**
   - Customer chỉ thấy data của mình
   - Employee chỉ thấy data của mình
   - Admin thấy tất cả + statistics
   - Manager không thấy data của Admin/Manager khác

## 🔄 Future Enhancements

1. **Filtering Statistics:**
   - Thêm query params `?from=2024-01-01&to=2024-12-31`
   - Statistics theo khoảng thời gian

2. **More Statistics:**
   - Customer: Average booking value, favorite room types
   - Employee: Performance metrics, average task completion time

3. **Caching:**
   - Cache statistics với TTL 5 minutes
   - Invalidate khi có update

4. **Export:**
   - Export summary to PDF
   - Export statistics to Excel

