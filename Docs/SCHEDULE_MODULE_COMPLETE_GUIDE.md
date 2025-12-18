# Schedule Management Module - Complete Documentation

## 📋 Mục lục
- [Tổng quan](#tổng-quan)
- [Kiến trúc Module](#kiến-trúc-module)
- [Database Schema](#database-schema)
- [API Endpoints](#api-endpoints)
- [Business Logic](#business-logic)
- [Frontend Integration](#frontend-integration)
- [Testing Guide](#testing-guide)

---

## 🎯 Tổng quan

Module **Schedule Management** quản lý lịch làm việc của nhân viên trong hệ thống khách sạn. Module hỗ trợ:

- ✅ Xem lịch làm việc theo khoảng thời gian (tuần/tháng)
- ✅ Thêm/Sửa/Xóa lịch làm việc
- ✅ Kiểm tra nhân viên có sẵn (không bị trùng lịch)
- ✅ Tự động group ca làm việc theo thời gian
- ✅ Phát hiện xung đột lịch làm việc
- ✅ Hỗ trợ nhiều ca làm việc linh hoạt

**Roles có quyền**: Admin, Manager

---

## 🏗️ Kiến trúc Module

### File Structure
```
AppBackend.ApiCore/
└── Controllers/
    └── ScheduleController.cs              # REST API endpoints

AppBackend.Services/
├── Services/
│   └── ScheduleServices/
│       ├── IScheduleService.cs            # Interface
│       └── ScheduleService.cs             # Business logic implementation
└── ApiModels/
    └── ScheduleModel/
        └── ScheduleApiModels.cs           # Request/Response DTOs

AppBackend.Repositories/
└── Repositories/
    └── EmployeeScheduleRepo/
        ├── IEmployeeScheduleRepository.cs # Repository interface
        └── EmployeeScheduleRepository.cs  # Data access implementation

AppBackend.BusinessObjects/
└── Models/
    └── EmployeeSchedule.cs                # Entity model
```

### Layer Responsibilities

#### 1. **Controller Layer** (ScheduleController.cs)
- Nhận HTTP requests
- Validate input với ModelState
- Authorize với JWT và Role-based
- Trả về HTTP responses

#### 2. **Service Layer** (ScheduleService.cs)
- Implement business logic:
  - Parse và validate date format
  - Check conflict lịch làm việc
  - Validate nhân viên status
  - Group shifts động theo thời gian
  - Determine shift names
- Orchestrate repository calls
- Return ResultModel với status codes

#### 3. **Repository Layer** (EmployeeScheduleRepository.cs)
- Data access với Entity Framework Core
- Query schedules với Include navigation properties
- Check conflicts trong database
- Get available employees

#### 4. **BusinessObjects Layer**
- Entity models (EmployeeSchedule, Employee)
- DTOs (Request/Response models)
- Constants và Enums

---

## 💾 Database Schema

### Table: EmployeeSchedule

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| ScheduleId | int | No | Primary Key (Identity) |
| EmployeeId | int | No | Foreign Key → Employee.EmployeeId |
| ShiftDate | date | No | Ngày làm việc |
| StartTime | time | No | Giờ bắt đầu ca |
| EndTime | time | No | Giờ kết thúc ca |
| Notes | nvarchar(255) | Yes | Ghi chú |
| CreatedAt | datetime2 | No | Thời gian tạo |
| CreatedBy | int | Yes | User tạo |
| UpdatedAt | datetime2 | Yes | Thời gian cập nhật |
| UpdatedBy | int | Yes | User cập nhật |

**Indexes:**
- `IX_EmployeeSchedule_EmployeeId` (EmployeeId)
- `IX_EmployeeSchedule_ShiftDate` (ShiftDate) - for date range queries
- Composite: (EmployeeId, ShiftDate, StartTime) - for conflict checking

**Foreign Keys:**
- `FK_EmployeeSchedule_Employee_EmployeeId` → Employee(EmployeeId) ON DELETE CASCADE

**Sample Data:**
```sql
INSERT INTO [EmployeeSchedule] 
  (EmployeeId, ShiftDate, StartTime, EndTime, Notes, CreatedAt, CreatedBy)
VALUES 
  (5, '2025-12-18', '06:00:00', '14:00:00', 'Ca sáng', GETUTCDATE(), 1),
  (5, '2025-12-19', '06:00:00', '14:00:00', 'Ca sáng', GETUTCDATE(), 1),
  (7, '2025-12-18', '14:00:00', '22:00:00', 'Ca chiều', GETUTCDATE(), 1);
```

---

## 🔌 API Endpoints

### Base URL: `/api/schedule`

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/schedules` | Lấy lịch theo khoảng thời gian | Admin, Manager |
| POST | `/` | Thêm lịch mới | Admin, Manager |
| PUT | `/{scheduleId}` | Cập nhật lịch | Admin, Manager |
| DELETE | `/{scheduleId}` | Xóa lịch | Admin, Manager |
| GET | `/available-employees` | Lấy nhân viên available | Admin, Manager |

---

## 📡 API Details

### 1. Get Schedules (Lấy lịch làm việc)

```http
POST /api/schedule/schedules
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**Request (Form-Data):**
```
fromDate: 20251216    // Format: yyyyMMdd
toDate: 20251222      // Format: yyyyMMdd
```

**Response Success (200):**
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
                "notes": "Ca sáng"
              }
            ]
          }
        ]
      }
    ]
  }
}
```

**Response Empty (200):**
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

**Validation Rules:**
- `fromDate` và `toDate` bắt buộc
- Format: `yyyyMMdd` (8 ký tự)
- `fromDate <= toDate`
- Khoảng thời gian tối đa: 31 ngày

---

### 2. Add Schedule (Thêm lịch mới)

```http
POST /api/schedule
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**Request (Form-Data):**
```
employeeId: 5
shiftDate: 2025-12-20
startTime: 06:00:00
endTime: 14:00:00
notes: Ca sáng thứ 6
```

**Response Success (201):**
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

**Validation & Business Rules:**
- ✅ Employee phải tồn tại
- ✅ Employee không được nghỉ việc (`terminationDate = null`)
- ✅ `startTime < endTime` (trừ ca đêm 22:00 - 06:00)
- ✅ Không được trùng lịch với schedule khác của cùng employee

**Error Responses:**
- 404: Employee không tồn tại
- 400: Employee đã nghỉ việc / Thời gian không hợp lệ
- 409: Trùng lịch làm việc

---

### 3. Update Schedule (Cập nhật lịch)

```http
PUT /api/schedule/{scheduleId}
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**Request (Form-Data) - All fields optional:**
```
employeeId: 7           // Optional
shiftDate: 2025-12-21   // Optional
startTime: 08:00:00     // Optional
endTime: 16:00:00       // Optional
notes: Đổi ca           // Optional
```

**Response Success (200):**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Updated successfully."
}
```

**Business Rules:**
- Chỉ update fields được gửi lên
- Validate giống như Add Schedule
- Kiểm tra conflict (exclude schedule đang update)

---

### 4. Delete Schedule (Xóa lịch)

```http
DELETE /api/schedule/{scheduleId}
Authorization: Bearer {token}
```

**Response Success (200):**
```json
{
  "isSuccess": true,
  "responseCode": "SUCCESS",
  "statusCode": 200,
  "message": "Deleted successfully."
}
```

**Error Response:**
- 404: Schedule không tồn tại

---

### 5. Get Available Employees (Lấy nhân viên rảnh)

```http
GET /api/schedule/available-employees?shiftDate=2025-12-20&startTime=06:00:00&endTime=14:00:00&employeeTypeId=1
Authorization: Bearer {token}
```

**Query Parameters:**
- `shiftDate` (required): yyyy-MM-dd
- `startTime` (required): HH:mm:ss
- `endTime` (required): HH:mm:ss
- `employeeTypeId` (optional): Filter by employee type

**Response Success (200):**
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
      }
    ]
  }
}
```

**Logic:**
- Lấy tất cả employees đang active
- Exclude employees đã có lịch trùng thời gian
- Filter theo employeeTypeId nếu có

---

## 🧠 Business Logic

### 1. Date Format Handling

**Input Format:** `yyyyMMdd` (8 ký tự)
- Ví dụ: `20251218` = 18/12/2025

**Parsing Logic:**
```csharp
int year = int.Parse(dateString.Substring(0, 4));
int month = int.Parse(dateString.Substring(4, 2));
int day = int.Parse(dateString.Substring(6, 2));
DateOnly date = new DateOnly(year, month, day);
```

### 2. Dynamic Shift Grouping

**Concept:** Không cần định nghĩa ca trước, hệ thống tự động group theo thời gian thực tế.

**Algorithm:**
```csharp
// 1. Lấy unique (StartTime, EndTime) pairs từ database
var uniqueShifts = schedules
    .Select(s => new { s.StartTime, s.EndTime })
    .Distinct()
    .OrderBy(s => s.StartTime);

// 2. Với mỗi unique shift, tạo ShiftScheduleDto
foreach (var shift in uniqueShifts)
{
    string shiftName = DetermineShiftName(shift.StartTime, shift.EndTime);
    // Group employees by date cho shift này
}
```

**Shift Name Generation:**
```csharp
private string DetermineShiftName(TimeOnly startTime, TimeOnly endTime)
{
    int hour = startTime.Hour;
    string baseName;
    
    if (hour >= 6 && hour < 14)
        baseName = "Ca Sáng";
    else if (hour >= 14 && hour < 22)
        baseName = "Ca Chiều";
    else
        baseName = "Ca Đêm";
    
    return $"{baseName} ({startTime:HH:mm} - {endTime:HH:mm})";
}
```

**Result:**
- `"Ca Sáng (06:00 - 14:00)"`
- `"Ca Sáng (08:00 - 16:00)"` ← Khác ca sáng
- `"Ca Chiều (14:00 - 22:00)"`
- `"Ca Đêm (22:00 - 06:00)"`

### 3. Conflict Detection

**Scenario:** Kiểm tra xem nhân viên có lịch trùng giờ không.

**Logic:**
```csharp
// Lịch trùng khi:
// 1. Cùng EmployeeId
// 2. Cùng ShiftDate
// 3. Thời gian overlap:
//    - startTime mới nằm trong [start, end] của lịch cũ
//    - endTime mới nằm trong [start, end] của lịch cũ
//    - Lịch mới bao phủ hoàn toàn lịch cũ

var hasConflict = await _context.EmployeeSchedules
    .Where(s => s.EmployeeId == employeeId 
             && s.ShiftDate == shiftDate
             && s.ScheduleId != excludeScheduleId)
    .AnyAsync(s => 
        (startTime >= s.StartTime && startTime < s.EndTime) ||
        (endTime > s.StartTime && endTime <= s.EndTime) ||
        (startTime <= s.StartTime && endTime >= s.EndTime)
    );
```

### 4. Schedule Status

**Logic:**
```csharp
private string DetermineScheduleStatus(EmployeeSchedule schedule)
{
    var today = DateOnly.FromDateTime(DateTime.Today);
    
    if (schedule.ShiftDate < today)
        return "Hoàn thành";
    else if (schedule.ShiftDate == today)
        return "Đang diễn ra";
    else
        return "Đã lên lịch";
}
```

**Status Values:**
- `"Đã lên lịch"`: Future dates
- `"Đang diễn ra"`: Today
- `"Hoàn thành"`: Past dates

### 5. Available Employees Query

**Logic:**
```csharp
// 1. Lấy tất cả employees đang active
var allEmployees = _context.Employees
    .Where(e => e.TerminationDate == null)
    .Include(e => e.EmployeeType);

// 2. Lọc theo employeeTypeId nếu có
if (employeeTypeId.HasValue)
    allEmployees = allEmployees.Where(e => e.EmployeeTypeId == employeeTypeId);

// 3. Exclude employees có lịch trùng giờ
var busyEmployeeIds = _context.EmployeeSchedules
    .Where(s => s.ShiftDate == shiftDate && (
        (startTime >= s.StartTime && startTime < s.EndTime) ||
        (endTime > s.StartTime && endTime <= s.EndTime) ||
        (startTime <= s.StartTime && endTime >= s.EndTime)
    ))
    .Select(s => s.EmployeeId);

var availableEmployees = allEmployees
    .Where(e => !busyEmployeeIds.Contains(e.EmployeeId));
```

---

## 🎨 Frontend Integration

### JavaScript Helper Functions

```javascript
// 1. Convert Date to yyyyMMdd
function toDateString(date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}${month}${day}`;
}

// 2. Convert yyyyMMdd to Date
function fromDateString(dateStr) {
  const year = dateStr.substring(0, 4);
  const month = dateStr.substring(4, 6);
  const day = dateStr.substring(6, 8);
  return new Date(`${year}-${month}-${day}`);
}

// 3. Get week range (Monday to Sunday)
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

// 4. API Service Class
class ScheduleService {
  constructor(baseUrl, token) {
    this.baseUrl = baseUrl;
    this.token = token;
  }

  async getSchedules(fromDate, toDate) {
    const formData = new FormData();
    formData.append('fromDate', fromDate);
    formData.append('toDate', toDate);

    const response = await fetch(`${this.baseUrl}/api/schedule/schedules`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`
      },
      body: formData
    });

    const result = await response.json();
    if (!result.isSuccess) throw new Error(result.message);
    return result.data;
  }

  async addSchedule(scheduleData) {
    const formData = new FormData();
    Object.keys(scheduleData).forEach(key => {
      if (scheduleData[key] !== null && scheduleData[key] !== undefined) {
        formData.append(key, scheduleData[key]);
      }
    });

    const response = await fetch(`${this.baseUrl}/api/schedule`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`
      },
      body: formData
    });

    const result = await response.json();
    if (!result.isSuccess) throw new Error(result.message);
    return result;
  }

  async updateSchedule(scheduleId, updateData) {
    const formData = new FormData();
    Object.keys(updateData).forEach(key => {
      if (updateData[key] !== null && updateData[key] !== undefined) {
        formData.append(key, updateData[key]);
      }
    });

    const response = await fetch(`${this.baseUrl}/api/schedule/${scheduleId}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${this.token}`
      },
      body: formData
    });

    const result = await response.json();
    if (!result.isSuccess) throw new Error(result.message);
    return result;
  }

  async deleteSchedule(scheduleId) {
    const response = await fetch(`${this.baseUrl}/api/schedule/${scheduleId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${this.token}`
      }
    });

    const result = await response.json();
    if (!result.isSuccess) throw new Error(result.message);
    return result;
  }

  async getAvailableEmployees(shiftDate, startTime, endTime, employeeTypeId = null) {
    const params = new URLSearchParams({
      shiftDate,
      startTime,
      endTime
    });
    if (employeeTypeId) params.append('employeeTypeId', employeeTypeId);

    const response = await fetch(
      `${this.baseUrl}/api/schedule/available-employees?${params}`,
      {
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      }
    );

    const result = await response.json();
    if (!result.isSuccess) throw new Error(result.message);
    return result.data;
  }
}
```

### React Example Component

```jsx
import React, { useState, useEffect } from 'react';

function WeeklySchedule() {
  const [schedules, setSchedules] = useState(null);
  const [currentWeek, setCurrentWeek] = useState(new Date());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const scheduleService = new ScheduleService(
    'http://localhost:8080',
    localStorage.getItem('token')
  );

  useEffect(() => {
    loadSchedules();
  }, [currentWeek]);

  const loadSchedules = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const { fromDate, toDate } = getWeekRange(currentWeek);
      const data = await scheduleService.getSchedules(fromDate, toDate);
      
      setSchedules(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handlePreviousWeek = () => {
    const newDate = new Date(currentWeek);
    newDate.setDate(newDate.getDate() - 7);
    setCurrentWeek(newDate);
  };

  const handleNextWeek = () => {
    const newDate = new Date(currentWeek);
    newDate.setDate(newDate.getDate() + 7);
    setCurrentWeek(newDate);
  };

  const handleAddSchedule = async (employeeId, date, shiftTime) => {
    try {
      await scheduleService.addSchedule({
        employeeId,
        shiftDate: date,
        startTime: shiftTime.start,
        endTime: shiftTime.end,
        notes: 'Added from UI'
      });
      loadSchedules(); // Reload
    } catch (err) {
      alert(err.message);
    }
  };

  const handleDeleteSchedule = async (scheduleId) => {
    if (!confirm('Xác nhận xóa lịch?')) return;
    
    try {
      await scheduleService.deleteSchedule(scheduleId);
      loadSchedules(); // Reload
    } catch (err) {
      alert(err.message);
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!schedules) return <div>No data</div>;

  return (
    <div className="schedule-container">
      <div className="schedule-header">
        <button onClick={handlePreviousWeek}>← Tuần trước</button>
        <h2>Lịch làm việc tuần {currentWeek.toLocaleDateString()}</h2>
        <button onClick={handleNextWeek}>Tuần sau →</button>
      </div>

      {schedules.shifts.map(shift => (
        <div key={shift.shiftName} className="shift-section">
          <h3>{shift.shiftName}</h3>
          
          <div className="daily-grid">
            {shift.dailySchedules.map(daily => (
              <div key={daily.shiftDate} className="day-column">
                <div className="day-header">
                  <div>{daily.dayOfWeek}</div>
                  <div>{daily.shiftDate}</div>
                </div>
                
                <div className="employees-list">
                  {daily.employees.map(emp => (
                    <div key={emp.scheduleId} className="employee-card">
                      <div className="emp-name">{emp.employeeName}</div>
                      <div className="emp-type">{emp.employeeType}</div>
                      <div className="emp-status">{emp.status}</div>
                      {emp.notes && <div className="emp-notes">{emp.notes}</div>}
                      <button 
                        onClick={() => handleDeleteSchedule(emp.scheduleId)}
                        className="btn-delete"
                      >
                        Xóa
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

export default WeeklySchedule;
```

---

## 🧪 Testing Guide

### 1. Manual Testing với Postman

#### Setup Environment
```
baseUrl: http://localhost:8080
token: <JWT_TOKEN_FROM_LOGIN>
```

#### Test Collection

**1.1. Login để lấy token**
```http
POST {{baseUrl}}/api/auth/login
Content-Type: multipart/form-data

username: admin@hotel.com
password: Admin@123
```

**1.2. Get schedules - Current week**
```http
POST {{baseUrl}}/api/schedule/schedules
Authorization: Bearer {{token}}
Content-Type: multipart/form-data

fromDate: 20251216
toDate: 20251222
```

**1.3. Get available employees**
```http
GET {{baseUrl}}/api/schedule/available-employees?shiftDate=2025-12-20&startTime=06:00:00&endTime=14:00:00&employeeTypeId=1
Authorization: Bearer {{token}}
```

**1.4. Add schedule**
```http
POST {{baseUrl}}/api/schedule
Authorization: Bearer {{token}}
Content-Type: multipart/form-data

employeeId: 5
shiftDate: 2025-12-20
startTime: 06:00:00
endTime: 14:00:00
notes: Test schedule
```

**1.5. Update schedule**
```http
PUT {{baseUrl}}/api/schedule/123
Authorization: Bearer {{token}}
Content-Type: multipart/form-data

startTime: 08:00:00
endTime: 16:00:00
notes: Updated schedule
```

**1.6. Delete schedule**
```http
DELETE {{baseUrl}}/api/schedule/123
Authorization: Bearer {{token}}
```

### 2. Test Scenarios

#### Scenario 1: Xem lịch tuần trống
```
Input: fromDate=20251230, toDate=20260105 (tuần không có data)
Expected: 
- Status 200
- Message: "Không có lịch làm việc trong khoảng thời gian này"
- data.shifts = []
```

#### Scenario 2: Thêm lịch thành công
```
Input:
  employeeId: 5
  shiftDate: 2025-12-25
  startTime: 06:00:00
  endTime: 14:00:00
Expected:
- Status 201
- Response có scheduleId
```

#### Scenario 3: Thêm lịch trùng (Conflict)
```
Input: Thêm lịch cho employee 5 vào 2025-12-25, 06:00-14:00 (đã có rồi)
Expected:
- Status 409
- Message: "Nhân viên đã có lịch làm việc trùng thời gian này"
```

#### Scenario 4: Thêm lịch cho nhân viên đã nghỉ việc
```
Input: 
  employeeId: 10 (đã có terminationDate)
  ...
Expected:
- Status 400
- Message: "Không thể thêm lịch cho nhân viên đã nghỉ việc"
```

#### Scenario 5: Format ngày sai
```
Input: fromDate=2025-12-16 (không đúng format yyyyMMdd)
Expected:
- Status 400
- Message: "Định dạng ngày không hợp lệ..."
```

#### Scenario 6: Khoảng thời gian > 31 ngày
```
Input: fromDate=20251201, toDate=20260110 (41 ngày)
Expected:
- Status 400
- Message: "...khoảng thời gian không quá 31 ngày"
```

#### Scenario 7: Multiple shifts cùng loại
```
Setup: 
- Tạo schedule 1: 06:00-14:00 (Ca sáng)
- Tạo schedule 2: 08:00-16:00 (Ca sáng)
Expected:
- API trả về 2 shifts riêng biệt:
  - "Ca Sáng (06:00 - 14:00)"
  - "Ca Sáng (08:00 - 16:00)"
```

### 3. Integration Testing

```csharp
[TestClass]
public class ScheduleServiceTests
{
    private IScheduleService _scheduleService;
    private Mock<IUnitOfWork> _mockUnitOfWork;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _scheduleService = new ScheduleService(_mockUnitOfWork.Object);
    }

    [TestMethod]
    public async Task GetWeeklyScheduleAsync_ValidDateRange_ReturnsSchedules()
    {
        // Arrange
        var request = new GetWeeklyScheduleRequest
        {
            FromDate = "20251216",
            ToDate = "20251222"
        };

        var mockSchedules = new List<EmployeeSchedule>
        {
            new EmployeeSchedule
            {
                ScheduleId = 1,
                EmployeeId = 5,
                ShiftDate = new DateOnly(2025, 12, 16),
                StartTime = new TimeOnly(6, 0),
                EndTime = new TimeOnly(14, 0),
                Employee = new Employee 
                { 
                    FullName = "Test Employee",
                    EmployeeType = new CommonCode { CodeValue = "Receptionist" }
                }
            }
        };

        _mockUnitOfWork.Setup(u => u.EmployeeSchedules.GetSchedulesByDateRangeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(mockSchedules);

        // Act
        var result = await _scheduleService.GetWeeklyScheduleAsync(request);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(200, result.StatusCode);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual(1, result.Data.Shifts.Count);
        Assert.AreEqual("Ca Sáng (06:00 - 14:00)", result.Data.Shifts[0].ShiftName);
    }

    [TestMethod]
    public async Task AddScheduleAsync_ConflictingSchedule_ReturnsConflictError()
    {
        // Arrange
        var request = new AddScheduleRequest
        {
            EmployeeId = 5,
            ShiftDate = new DateOnly(2025, 12, 20),
            StartTime = new TimeOnly(6, 0),
            EndTime = new TimeOnly(14, 0)
        };

        _mockUnitOfWork.Setup(u => u.Employees.GetByIdAsync(5))
            .ReturnsAsync(new Employee { EmployeeId = 5, TerminationDate = null });

        _mockUnitOfWork.Setup(u => u.EmployeeSchedules.HasConflictingScheduleAsync(
            5, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null))
            .ReturnsAsync(true);

        // Act
        var result = await _scheduleService.AddScheduleAsync(request, 1);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(409, result.StatusCode);
        Assert.AreEqual("EXISTED", result.ResponseCode);
    }
}
```

### 4. Load Testing

**Scenario:** 100 concurrent users xem lịch tuần

```bash
# Using Apache Bench
ab -n 1000 -c 100 -H "Authorization: Bearer TOKEN" \
   -p schedule_request.txt -T "multipart/form-data" \
   http://localhost:8080/api/schedule/schedules

# schedule_request.txt content:
fromDate=20251216&toDate=20251222
```

**Expected Performance:**
- Response time: < 500ms (p95)
- Throughput: > 200 req/s
- Error rate: < 1%

---

## 🔒 Security Considerations

### 1. Authorization
- Tất cả endpoints yêu cầu JWT token
- Chỉ Admin và Manager có quyền access
- Employee không thể xem/sửa lịch của người khác (chưa implement)

### 2. Input Validation
- Validate date format ngay từ đầu
- Check date range không quá 31 ngày (prevent large queries)
- Sanitize Notes field (max 255 chars)

### 3. SQL Injection Prevention
- Sử dụng Entity Framework Core (parameterized queries)
- Không có raw SQL trong code

### 4. Rate Limiting
```csharp
// Recommended: Add rate limiting attribute
[RateLimit(100, 60)] // 100 requests per 60 seconds
public async Task<IActionResult> GetSchedules(...)
```

---

## 📊 Performance Optimization

### 1. Database Indexing
```sql
-- Composite index cho conflict checking
CREATE INDEX IX_EmployeeSchedule_EmployeeId_ShiftDate_StartTime 
ON EmployeeSchedule(EmployeeId, ShiftDate, StartTime);

-- Index cho date range queries
CREATE INDEX IX_EmployeeSchedule_ShiftDate 
ON EmployeeSchedule(ShiftDate);
```

### 2. Query Optimization
```csharp
// Include navigation properties để tránh N+1 queries
var schedules = await Context.EmployeeSchedules
    .Include(es => es.Employee)
        .ThenInclude(e => e.EmployeeType)
    .Where(es => es.ShiftDate >= startDate && es.ShiftDate <= endDate)
    .OrderBy(es => es.ShiftDate)
        .ThenBy(es => es.StartTime)
    .ToListAsync();
```

### 3. Caching Strategy
```csharp
// Cache lịch tuần hiện tại
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<IActionResult> GetSchedules(...)

// Hoặc dùng Memory Cache
_cache.Set($"schedule_{fromDate}_{toDate}", data, TimeSpan.FromMinutes(5));
```

### 4. Pagination
```csharp
// Nếu có quá nhiều schedules, implement pagination
public async Task<PagedResult<ScheduleDto>> GetSchedules(
    GetScheduleRequest request, 
    int page = 1, 
    int pageSize = 100)
{
    var query = _context.EmployeeSchedules
        .Where(s => s.ShiftDate >= request.StartDate 
                 && s.ShiftDate <= request.EndDate);
    
    var total = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<ScheduleDto>
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}
```

---

## 🚀 Future Enhancements

### 1. Recurring Schedules
```csharp
// Thêm khả năng tạo lịch lặp lại
public class RecurringScheduleRequest
{
    public int EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } // [Monday, Wednesday, Friday]
}
```

### 2. Shift Templates
```csharp
// Define shift templates
public class ShiftTemplate
{
    public int TemplateId { get; set; }
    public string Name { get; set; } // "Morning Shift"
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

// Apply template to employees
POST /api/schedule/apply-template
{
    templateId: 1,
    employeeIds: [5, 7, 9],
    dates: ["2025-12-20", "2025-12-21"]
}
```

### 3. Shift Swap
```csharp
// Employee request to swap shifts
POST /api/schedule/swap-request
{
    fromScheduleId: 123,
    toEmployeeId: 7,
    reason: "Personal emergency"
}

// Manager approve/reject
PUT /api/schedule/swap-request/{requestId}/approve
```

### 4. Attendance Integration
```csharp
// Link schedules with attendance
public class Attendance
{
    public int AttendanceId { get; set; }
    public int ScheduleId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } // OnTime, Late, Absent
}
```

### 5. Real-time Notifications
```csharp
// WebSocket/SignalR để notify employees về lịch mới
public async Task NotifyScheduleChanged(int employeeId, string message)
{
    await _hubContext.Clients.User(employeeId.ToString())
        .SendAsync("ScheduleUpdated", message);
}
```

### 6. Export to Calendar
```csharp
// Export to iCal format
GET /api/schedule/export/ical?employeeId=5&fromDate=20251216&toDate=20251222

// Response: .ics file
```

### 7. Analytics Dashboard
```csharp
GET /api/schedule/analytics?month=12&year=2025

Response:
{
    totalSchedules: 350,
    employeeUtilization: {
        5: 85%, // 85% of working days
        7: 92%
    },
    shiftDistribution: {
        "Morning": 120,
        "Afternoon": 115,
        "Night": 115
    }
}
```

---

## 📚 Related Documentation

- [SCHEDULE_API_DOCUMENTATION.md](./SCHEDULE_API_DOCUMENTATION.md) - API endpoints chi tiết với examples
- [EMPLOYEE_API_DOCUMENTATION.md](./EMPLOYEE_API_DOCUMENTATION.md) - Quản lý nhân viên
- [AUTHENTICATION_GUIDE.md](./AUTHENTICATION_GUIDE.md) - JWT authentication

---

## 🆘 Troubleshooting

### Issue 1: "Không có lịch làm việc trong khoảng thời gian này"
**Cause:** Database không có data hoặc date range sai
**Solution:**
1. Check database: `SELECT * FROM EmployeeSchedule WHERE ShiftDate BETWEEN '2025-12-16' AND '2025-12-22'`
2. Verify date format: `fromDate=20251216, toDate=20251222`
3. Insert sample data bằng SQL script

### Issue 2: 409 Conflict - Trùng lịch
**Cause:** Employee đã có lịch trùng giờ
**Solution:**
1. Check existing schedules: `GET /api/schedule/schedules`
2. Xóa hoặc update schedule cũ trước
3. Hoặc chọn employee khác

### Issue 3: 401 Unauthorized
**Cause:** Token expired hoặc không hợp lệ
**Solution:**
1. Login lại để lấy token mới
2. Check token expiration time
3. Verify role (phải là Admin hoặc Manager)

### Issue 4: Performance chậm khi query large date range
**Cause:** Query quá nhiều records
**Solution:**
1. Giới hạn date range (đã có: max 31 ngày)
2. Add pagination
3. Add database indexes
4. Enable caching

---

## 📝 Change Log

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 2.0.0 | 2025-12-18 | - Đổi GET sang POST với form-data<br>- Thay `date` bằng `fromDate/toDate`<br>- Dynamic shift grouping<br>- Validation khoảng thời gian max 31 ngày | Backend Team |
| 1.1.0 | 2025-12-15 | - Add available employees endpoint<br>- Conflict detection | Backend Team |
| 1.0.0 | 2025-12-14 | Initial release | Backend Team |

---

## 💬 Support & Contact

**Backend Team:**
- Email: backend@hotel.com
- Slack: #backend-support
- Issue Tracker: https://github.com/hotel-system/issues

**Documentation Updates:**
- Submit PR to update docs
- Follow documentation standards

---

**Last Updated:** December 18, 2025
**Module Version:** 2.0.0
**API Version:** v1

