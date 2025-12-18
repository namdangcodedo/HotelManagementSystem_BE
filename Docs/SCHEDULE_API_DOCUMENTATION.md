# Schedule API Documentation

## Tổng quan
API Schedule quản lý lịch làm việc của nhân viên trong hệ thống khách sạn. API hỗ trợ xem lịch theo khoảng thời gian, thêm/sửa/xóa lịch làm việc và kiểm tra nhân viên có sẵn.

**Base URL**: `/api/schedule`

**Authentication**: Tất cả endpoints yêu cầu JWT token và role Admin hoặc Manager.

---

## 1. Lấy lịch làm việc theo khoảng thời gian

### Endpoint
```
POST /api/schedule/schedules
```

### Authorization
- **Roles**: Admin, Manager
- **Headers**: `Authorization: Bearer {token}`

### Request (Form-Data)

| Field | Type | Required | Format | Description | Example |
|-------|------|----------|--------|-------------|---------|
| fromDate | string | Yes | yyyyMMdd | Ngày bắt đầu | 20251216 |
| toDate | string | Yes | yyyyMMdd | Ngày kết thúc | 20251222 |
| employeeTypeId | integer | No | - | Lọc theo loại nhân viên | 1 |

**Lưu ý**:
- Format ngày: `yyyyMMdd` (8 ký tự)
- `fromDate` phải <= `toDate`
- Khoảng thời gian tối đa: 31 ngày
- Thường dùng để lấy lịch theo tuần (7 ngày)
- `employeeTypeId` (optional): Filter theo loại nhân viên (VD: 1=Receptionist, 2=Housekeeper, 3=Manager)

### Example Request (cURL)
```bash
# Lấy lịch tất cả nhân viên
curl -X POST "http://localhost:8080/api/schedule/schedules" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "fromDate=20251216" \
  -F "toDate=20251222"

# Lấy lịch chỉ Receptionist (employeeTypeId=1)
curl -X POST "http://localhost:8080/api/schedule/schedules" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "fromDate=20251216" \
  -F "toDate=20251222" \
  -F "employeeTypeId=1"
```

### Example Request (JavaScript/Fetch)
```javascript
// Lấy tất cả
const formData = new FormData();
formData.append('fromDate', '20251216');
formData.append('toDate', '20251222');

// Hoặc filter theo loại nhân viên
formData.append('employeeTypeId', '1'); // Receptionist only

const response = await fetch('http://localhost:8080/api/schedule/schedules', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token
  },
  body: formData
});

const data = await response.json();
```

### Success Response (200 OK)

**Có dữ liệu**:
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Retrieved successfully.",
  "data": {
    "shifts": [
      {
        "shiftName": "Ca Sáng (06:00 - 14:00)",
        "startTime": "06:00:00",
        "endTime": "14:00:00",
        "dailySchedules": [
          {
            "shiftDate": "2025-12-16",
            "dayOfWeek": "Thứ 2",
            "employees": [
              {
                "scheduleId": 1,
                "employeeId": 5,
                "employeeName": "Nguyễn Văn A",
                "employeeType": "Receptionist",
                "status": "Đã lên lịch",
                "notes": "Ca sáng thứ 2"
              },
              {
                "scheduleId": 2,
                "employeeId": 7,
                "employeeName": "Trần Thị B",
                "employeeType": "Housekeeper",
                "status": "Đã lên lịch",
                "notes": null
              }
            ]
          },
          {
            "shiftDate": "2025-12-17",
            "dayOfWeek": "Thứ 3",
            "employees": [
              {
                "scheduleId": 5,
                "employeeId": 5,
                "employeeName": "Nguyễn Văn A",
                "employeeType": "Receptionist",
                "status": "Đã lên lịch",
                "notes": null
              }
            ]
          },
          {
            "shiftDate": "2025-12-18",
            "dayOfWeek": "Thứ 4",
            "employees": []
          }
        ]
      },
      {
        "shiftName": "Ca Chiều (14:00 - 22:00)",
        "startTime": "14:00:00",
        "endTime": "22:00:00",
        "dailySchedules": [
          {
            "shiftDate": "2025-12-16",
            "dayOfWeek": "Thứ 2",
            "employees": [
              {
                "scheduleId": 3,
                "employeeId": 8,
                "employeeName": "Lê Văn C",
                "employeeType": "Receptionist",
                "status": "Đã lên lịch",
                "notes": "Ca chiều"
              }
            ]
          }
        ]
      },
      {
        "shiftName": "Ca Sáng (08:00 - 16:00)",
        "startTime": "08:00:00",
        "endTime": "16:00:00",
        "dailySchedules": [
          {
            "shiftDate": "2025-12-17",
            "dayOfWeek": "Thứ 3",
            "employees": [
              {
                "scheduleId": 6,
                "employeeId": 9,
                "employeeName": "Phạm Thị D",
                "employeeType": "Housekeeper",
                "status": "Đã lên lịch",
                "notes": "Ca sáng 8h-16h"
              }
            ]
          }
        ]
      }
    ]
  }
}
```

**Không có dữ liệu**:
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Không có lịch làm việc trong khoảng thời gian này",
  "data": {
    "shifts": []
  }
}
```

### Response Fields Explanation

#### Shifts Array
- `shiftName`: Tên ca kèm thời gian (tự động group theo startTime và endTime)
- `startTime`: Giờ bắt đầu ca (format HH:mm:ss)
- `endTime`: Giờ kết thúc ca (format HH:mm:ss)
- `dailySchedules`: Mảng lịch làm việc theo từng ngày

#### Daily Schedules
- `shiftDate`: Ngày làm việc (format yyyy-MM-dd)
- `dayOfWeek`: Thứ trong tuần (tiếng Việt)
- `employees`: Mảng nhân viên làm ca này trong ngày này

#### Employee Schedule
- `scheduleId`: ID của lịch làm việc (dùng để update/delete)
- `employeeId`: ID nhân viên
- `employeeName`: Tên đầy đủ của nhân viên
- `employeeType`: Loại nhân viên (Receptionist, Housekeeper, Manager, etc.)
- `status`: Trạng thái lịch
  - `"Đã lên lịch"`: Chưa tới ngày làm việc
  - `"Đang diễn ra"`: Đang trong ngày làm việc
  - `"Hoàn thành"`: Đã qua ngày làm việc
- `notes`: Ghi chú (có thể null)

### Error Responses

**400 Bad Request - Format ngày không hợp lệ**:
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "Định dạng ngày không hợp lệ. Vui lòng sử dụng format yyyyMMdd (VD: 20251216)"
}
```

**400 Bad Request - Dữ liệu không hợp lệ**:
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "Dữ liệu không hợp lệ. StartDate phải <= EndDate và khoảng thời gian không quá 31 ngày"
}
```

**401 Unauthorized**:
```json
{
  "isSuccess": false,
  "responseCode": "UNAUTHORIZED",
  "statusCode": 401,
  "message": "Unauthorized"
}
```

---

## 2. Thêm lịch làm việc mới

### Endpoint
```
POST /api/schedule
```

### Authorization
- **Roles**: Admin, Manager
- **Headers**: `Authorization: Bearer {token}`

### Request (Form-Data)

| Field | Type | Required | Format | Description | Example |
|-------|------|----------|--------|-------------|---------|
| employeeId | integer | Yes | - | ID nhân viên | 5 |
| shiftDate | string | Yes | yyyy-MM-dd | Ngày làm việc | 2025-12-20 |
| startTime | string | Yes | HH:mm:ss | Giờ bắt đầu | 06:00:00 |
| endTime | string | Yes | HH:mm:ss | Giờ kết thúc | 14:00:00 |
| notes | string | No | - | Ghi chú (max 255 ký tự) | Ca sáng |

### Example Request (cURL)
```bash
curl -X POST "http://localhost:8080/api/schedule" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "employeeId=5" \
  -F "shiftDate=2025-12-20" \
  -F "startTime=06:00:00" \
  -F "endTime=14:00:00" \
  -F "notes=Ca sáng thứ 6"
```

### Example Request (JavaScript/Fetch)
```javascript
const formData = new FormData();
formData.append('employeeId', '5');
formData.append('shiftDate', '2025-12-20');
formData.append('startTime', '06:00:00');
formData.append('endTime', '14:00:00');
formData.append('notes', 'Ca sáng thứ 6');

const response = await fetch('http://localhost:8080/api/schedule', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token
  },
  body: formData
});

const data = await response.json();
```

### Success Response (201 Created)
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 201,
  "message": "Created successfully.",
  "data": {
    "scheduleId": 123
  }
}
```

### Error Responses

**404 Not Found - Nhân viên không tồn tại**:
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "message": "Nhân viên không tìm thấy"
}
```

**400 Bad Request - Nhân viên đã nghỉ việc**:
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "Không thể thêm lịch cho nhân viên đã nghỉ việc"
}
```

**400 Bad Request - Thời gian không hợp lệ**:
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc"
}
```

**409 Conflict - Trùng lịch**:
```json
{
  "isSuccess": false,
  "responseCode": "EXISTED",
  "statusCode": 409,
  "message": "Nhân viên đã có lịch làm việc trùng thời gian này"
}
```

---

## 3. Cập nhật lịch làm việc

### Endpoint
```
PUT /api/schedule/{scheduleId}
```

### Authorization
- **Roles**: Admin, Manager
- **Headers**: `Authorization: Bearer {token}`

### Path Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| scheduleId | integer | Yes | ID của lịch làm việc cần cập nhật |

### Request (Form-Data) - All fields optional:

| Field | Type | Required | Format | Description | Example |
|-------|------|----------|--------|-------------|---------|
| employeeId | integer | No | - | ID nhân viên mới | 7 |
| shiftDate | string | No | yyyy-MM-dd hoặc yyyyMMdd | Ngày làm việc mới | 2025-12-21 hoặc 20251221 |
| startTime | string | No | HH:mm:ss hoặc HH:mm | Giờ bắt đầu mới | 08:00:00 hoặc 08:00 |
| endTime | string | No | HH:mm:ss hoặc HH:mm | Giờ kết thúc mới | 16:00:00 hoặc 16:00 |
| notes | string | No | - | Ghi chú mới (max 255 ký tự) | Đổi ca |

**Lưu ý**: 
- Tất cả fields đều optional, chỉ cần gửi field nào muốn cập nhật
- `shiftDate` hỗ trợ 2 format: `yyyy-MM-dd` (2025-12-21) hoặc `yyyyMMdd` (20251221)
- `startTime` và `endTime` hỗ trợ 2 format: `HH:mm:ss` (08:00:00) hoặc `HH:mm` (08:00)

**Example Request (cURL):**
```bash
curl -X PUT "http://localhost:8080/api/schedule/123" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -F "employeeId=6" \
  -F "shiftDate=2025-12-15" \
  -F "startTime=00:00:00" \
  -F "endTime=08:00:00"
```

**Example Request (JavaScript/Fetch):**
```javascript
const formData = new FormData();
formData.append('employeeId', '6');
formData.append('shiftDate', '2025-12-15');  // hoặc '20251215'
formData.append('startTime', '00:00:00');     // hoặc '00:00'
formData.append('endTime', '08:00:00');       // hoặc '08:00'

const response = await fetch('http://localhost:8080/api/schedule/1', {
  method: 'PUT',
  headers: {
    'Authorization': 'Bearer ' + token
  },
  body: formData
});

const data = await response.json();
```

**Success Response (200 OK):**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Updated successfully."
}
```

**Error Responses:**

**400 Bad Request - Format không hợp lệ:**
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "StartTime phải có format HH:mm:ss hoặc HH:mm"
}
```

**404 Not Found - Lịch không tồn tại:**
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "message": "Lịch làm việc không tìm thấy"
}
```

**404 Not Found - Nhân viên mới không tồn tại:**
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "message": "Nhân viên không tìm thấy"
}
```

**409 Conflict - Trùng lịch sau khi update:**
```json
{
  "isSuccess": false,
  "responseCode": "EXISTED",
  "statusCode": 409,
  "message": "Nhân viên đã có lịch làm việc trùng thời gian này"
}
```

---

## 4. Xóa lịch làm việc

### Endpoint
```
DELETE /api/schedule/{scheduleId}
```

### Authorization
- **Roles**: Admin, Manager
- **Headers**: `Authorization: Bearer {token}`

### Path Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| scheduleId | integer | Yes | ID của lịch làm việc cần xóa |

### Example Request (cURL)
```bash
curl -X DELETE "http://localhost:8080/api/schedule/123" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Example Request (JavaScript/Fetch)
```javascript
const response = await fetch('http://localhost:8080/api/schedule/123', {
  method: 'DELETE',
  headers: {
    'Authorization': 'Bearer ' + token
  }
});

const data = await response.json();
```

### Success Response (200 OK)
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Deleted successfully."
}
```

### Error Response

**404 Not Found**:
```json
{
  "isSuccess": false,
  "responseCode": "NOT_FOUND",
  "statusCode": 404,
  "message": "Lịch làm việc không tìm thấy"
}
```

---

## 5. Lấy danh sách nhân viên có sẵn

Kiểm tra nhân viên nào không bị trùng lịch trong khoảng thời gian cụ thể (để thêm vào ca làm việc).

### Endpoint
```
GET /api/schedule/available-employees
```

### Authorization
- **Roles**: Admin, Manager
- **Headers**: `Authorization: Bearer {token}`

### Query Parameters

| Parameter | Type | Required | Format | Description | Example |
|-----------|------|----------|--------|-------------|---------|
| shiftDate | string | Yes | yyyy-MM-dd | Ngày làm việc | 2025-12-20 |
| startTime | string | Yes | HH:mm:ss | Giờ bắt đầu | 06:00:00 |
| endTime | string | Yes | HH:mm:ss | Giờ kết thúc | 14:00:00 |
| employeeTypeId | integer | No | - | Lọc theo loại nhân viên | 1 |

### Example Request (cURL)
```bash
curl -X GET "http://localhost:8080/api/schedule/available-employees?shiftDate=2025-12-20&startTime=06:00:00&endTime=14:00:00&employeeTypeId=1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Example Request (JavaScript/Fetch)
```javascript
const params = new URLSearchParams({
  shiftDate: '2025-12-20',
  startTime: '06:00:00',
  endTime: '14:00:00',
  employeeTypeId: '1'  // optional
});

const response = await fetch(`http://localhost:8080/api/schedule/available-employees?${params}`, {
  method: 'GET',
  headers: {
    'Authorization': 'Bearer ' + token
  }
});

const data = await response.json();
```

### Success Response (200 OK)
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Retrieved successfully.",
  "data": {
    "employees": [
      {
        "employeeId": 5,
        "fullName": "Nguyễn Văn A",
        "employeeType": "Receptionist",
        "employeeTypeId": 1,
        "phoneNumber": "0123456789"
      },
      {
        "employeeId": 7,
        "fullName": "Trần Thị B",
        "employeeType": "Receptionist",
        "employeeTypeId": 1,
        "phoneNumber": "0987654321"
      }
    ]
  }
}
```

### Error Response

**400 Bad Request**:
```json
{
  "isSuccess": false,
  "responseCode": "INVALID",
  "statusCode": 400,
  "message": "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc"
}
```

---

## Use Cases & Examples

### Use Case 1: Xem lịch tuần hiện tại (16/12/2025 - 22/12/2025)

```javascript
// Tính toán fromDate và toDate cho tuần hiện tại
const today = new Date('2025-12-18'); // Thứ 4
const dayOfWeek = today.getDay(); // 3 (Wednesday)
const monday = new Date(today);
monday.setDate(today.getDate() - (dayOfWeek === 0 ? 6 : dayOfWeek - 1));
const sunday = new Date(monday);
sunday.setDate(monday.getDate() + 6);

const fromDate = monday.toISOString().slice(0,10).replace(/-/g,''); // 20251216
const toDate = sunday.toISOString().slice(0,10).replace(/-/g,'');   // 20251222

// Gọi API
const formData = new FormData();
formData.append('fromDate', fromDate);
formData.append('toDate', toDate);

const response = await fetch('http://localhost:8080/api/schedule/schedules', {
  method: 'POST',
  headers: { 'Authorization': 'Bearer ' + token },
  body: formData
});

const schedules = await response.json();
```

### Use Case 2: Thêm nhiều nhân viên vào cùng một ca

```javascript
// Bước 1: Kiểm tra nhân viên available
const params = new URLSearchParams({
  shiftDate: '2025-12-20',
  startTime: '06:00:00',
  endTime: '14:00:00',
  employeeTypeId: '1' // Receptionist
});

const availableRes = await fetch(
  `http://localhost:8080/api/schedule/available-employees?${params}`,
  { headers: { 'Authorization': 'Bearer ' + token } }
);
const { data: { employees } } = await availableRes.json();

// Bước 2: Thêm từng nhân viên vào ca
for (const employee of employees.slice(0, 3)) { // Lấy 3 nhân viên đầu
  const formData = new FormData();
  formData.append('employeeId', employee.employeeId);
  formData.append('shiftDate', '2025-12-20');
  formData.append('startTime', '06:00:00');
  formData.append('endTime', '14:00:00');
  formData.append('notes', 'Ca sáng thứ 6');
  
  await fetch('http://localhost:8080/api/schedule', {
    method: 'POST',
    headers: { 'Authorization': 'Bearer ' + token },
    body: formData
  });
}
```

### Use Case 3: Tạo lịch cho cả tuần

```javascript
const schedules = [
  { employeeId: 5, date: '2025-12-16', shift: 'morning' },
  { employeeId: 5, date: '2025-12-17', shift: 'morning' },
  { employeeId: 7, date: '2025-12-16', shift: 'afternoon' },
  { employeeId: 7, date: '2025-12-18', shift: 'afternoon' },
];

const shiftTimes = {
  morning: { start: '06:00:00', end: '14:00:00' },
  afternoon: { start: '14:00:00', end: '22:00:00' },
  night: { start: '22:00:00', end: '06:00:00' }
};

for (const schedule of schedules) {
  const formData = new FormData();
  formData.append('employeeId', schedule.employeeId);
  formData.append('shiftDate', schedule.date);
  formData.append('startTime', shiftTimes[schedule.shift].start);
  formData.append('endTime', shiftTimes[schedule.shift].end);
  
  await fetch('http://localhost:8080/api/schedule', {
    method: 'POST',
    headers: { 'Authorization': 'Bearer ' + token },
    body: formData
  });
}
```

### Use Case 4: Hiển thị lịch theo dạng bảng (Calendar Grid)

```javascript
// Fetch data
const formData = new FormData();
formData.append('fromDate', '20251216');
formData.append('toDate', '20251222');

const response = await fetch('http://localhost:8080/api/schedule/schedules', {
  method: 'POST',
  headers: { 'Authorization': 'Bearer ' + token },
  body: formData
});

const { data: { shifts } } = await response.json();

// Transform data cho calendar view
const calendarData = {};

shifts.forEach(shift => {
  shift.dailySchedules.forEach(daily => {
    const date = daily.shiftDate;
    if (!calendarData[date]) {
      calendarData[date] = {
        date: date,
        dayOfWeek: daily.dayOfWeek,
        shifts: []
      };
    }
    
    calendarData[date].shifts.push({
      shiftName: shift.shiftName,
      startTime: shift.startTime,
      endTime: shift.endTime,
      employees: daily.employees
    });
  });
});

// Render calendar
Object.values(calendarData).forEach(day => {
  console.log(`\n${day.dayOfWeek} - ${day.date}`);
  day.shifts.forEach(shift => {
    console.log(`  ${shift.shiftName}`);
    shift.employees.forEach(emp => {
      console.log(`    - ${emp.employeeName} (${emp.employeeType})`);
    });
  });
});
```

---

## Business Rules

### 1. Thời gian làm việc
- `startTime` phải nhỏ hơn `endTime` (trừ ca đêm qua ngày: 22:00 - 06:00)
- Ca đêm có `endTime = 06:00:00` được phép có `startTime = 22:00:00`

### 2. Xung đột lịch
- Một nhân viên không thể có 2 lịch trùng thời gian trong cùng ngày
- Hệ thống tự động kiểm tra conflict khi thêm/cập nhật

### 3. Trạng thái nhân viên
- Chỉ thêm lịch cho nhân viên đang hoạt động (`terminationDate = null`)
- Không thể thêm lịch cho nhân viên đã nghỉ việc

### 4. Phân ca động
- Hệ thống tự động group lịch theo cặp (startTime, endTime)
- Không cần định nghĩa ca trước, chỉ cần tạo lịch với thời gian cụ thể
- Tên ca được tự động tạo: "Ca Sáng (06:00 - 14:00)"

### 5. Khoảng thời gian
- API hỗ trợ xem lịch tối đa 31 ngày
- Thường dùng để xem theo tuần (7 ngày) hoặc tháng

---

## Error Codes Reference

| Response Code | Status Code | Description |
|---------------|-------------|-------------|
| SUCCESS | 200/201 | Thành công |
| INVALID | 400 | Dữ liệu đầu vào không hợp lệ |
| UNAUTHORIZED | 401 | Chưa đăng nhập hoặc token hết hạn |
| FORBIDDEN | 403 | Không có quyền truy cập |
| NOT_FOUND | 404 | Không tìm thấy resource |
| EXISTED | 409 | Dữ liệu đã tồn tại (conflict) |
| ERROR | 500 | Lỗi server |

---

## Testing với Postman

### 1. Import Collection
Tạo collection mới với các request sau:

**Environment Variables**:
```
baseUrl: http://localhost:8080
token: <your_jwt_token>
```

### 2. Test Flow

#### Step 1: Login để lấy token
```
POST {{baseUrl}}/api/auth/login
Body (form-data):
  username: admin@hotel.com
  password: Admin@123
```

#### Step 2: Xem lịch tuần này
```
POST {{baseUrl}}/api/schedule/schedules
Headers:
  Authorization: Bearer {{token}}
Body (form-data):
  fromDate: 20251216
  toDate: 20251222
```

#### Step 3: Kiểm tra nhân viên available
```
GET {{baseUrl}}/api/schedule/available-employees?shiftDate=2025-12-20&startTime=06:00:00&endTime=14:00:00
Headers:
  Authorization: Bearer {{token}}
```

#### Step 4: Thêm lịch mới
```
POST {{baseUrl}}/api/schedule
Headers:
  Authorization: Bearer {{token}}
Body (form-data):
  employeeId: 5
  shiftDate: 2025-12-20
  startTime: 06:00:00
  endTime: 14:00:00
  notes: Test schedule
```

#### Step 5: Cập nhật lịch
```
PUT {{baseUrl}}/api/schedule/123
Headers:
  Authorization: Bearer {{token}}
Body (form-data):
  notes: Updated notes
```

#### Step 6: Xóa lịch
```
DELETE {{baseUrl}}/api/schedule/123
Headers:
  Authorization: Bearer {{token}}
```

---

## Notes for Frontend Developers

### 1. Date Format Handling
```javascript
// Convert Date object to yyyyMMdd
function toDateString(date) {
  return date.toISOString().slice(0,10).replace(/-/g,'');
}

// Convert yyyyMMdd to Date object
function fromDateString(dateStr) {
  const year = dateStr.substring(0, 4);
  const month = dateStr.substring(4, 6);
  const day = dateStr.substring(6, 8);
  return new Date(`${year}-${month}-${day}`);
}
```

### 2. Week Calculation
```javascript
function getWeekRange(date) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1);
  const monday = new Date(d.setDate(diff));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  
  return {
    fromDate: toDateString(monday),
    toDate: toDateString(sunday)
  };
}
```

### 3. Form Data Helper
```javascript
function createScheduleFormData(data) {
  const formData = new FormData();
  Object.keys(data).forEach(key => {
    if (data[key] !== null && data[key] !== undefined) {
      formData.append(key, data[key]);
    }
  });
  return formData;
}

// Usage
const formData = createScheduleFormData({
  employeeId: 5,
  shiftDate: '2025-12-20',
  startTime: '06:00:00',
  endTime: '14:00:00',
  notes: 'Ca sáng'
});
```

### 4. Error Handling
```javascript
async function fetchSchedules(fromDate, toDate) {
  try {
    const formData = new FormData();
    formData.append('fromDate', fromDate);
    formData.append('toDate', toDate);
    
    const response = await fetch('/api/schedule/schedules', {
      method: 'POST',
      headers: { 'Authorization': 'Bearer ' + token },
      body: formData
    });
    
    const result = await response.json();
    
    if (!result.isSuccess) {
      throw new Error(result.message);
    }
    
    return result.data;
  } catch (error) {
    console.error('Failed to fetch schedules:', error);
    throw error;
  }
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0 | 2025-12-18 | - Thay đổi endpoint GET thành POST với form-data<br>- Đổi input từ `date` sang `fromDate/toDate`<br>- Thêm validation khoảng thời gian max 31 ngày<br>- Loại bỏ logic tính tuần tự động<br>- Cập nhật tất cả ví dụ |
| 1.0 | 2025-12-14 | Initial release |

---

## Support

Nếu có vấn đề hoặc câu hỏi, vui lòng liên hệ:
- **Backend Team**: backend@hotel.com
- **Documentation**: docs@hotel.com
