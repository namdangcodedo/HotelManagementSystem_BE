# 📋 SALARY MODULE DOCUMENTATION

## 1. Tổng quan

Module Salary quản lý thông tin lương và tính lương cho nhân viên khách sạn, bao gồm:
- Quản lý thông tin lương cơ bản theo năm (SalaryInfo)
- Tính lương hàng tháng dựa trên chấm công (Attendance)
- Xuất file Excel bảng lương

---

## 2. Cấu trúc Database

### 2.1 SalaryInfo (Thông tin lương theo năm)

| Field | Type | Mô tả |
|-------|------|-------|
| SalaryInfoId | int | Primary Key |
| EmployeeId | int | FK → Employee |
| Year | int | Năm áp dụng |
| BaseSalary | decimal | Lương cơ bản |
| YearBonus | decimal? | Thưởng năm |
| Allowance | decimal? | Phụ cấp |
| CreatedAt | DateTime? | Ngày tạo |
| UpdatedAt | DateTime? | Ngày cập nhật |

### 2.2 SalaryRecord (Bản ghi lương đã tính)

| Field | Type | Mô tả |
|-------|------|-------|
| SalaryRecordId | int | Primary Key |
| EmployeeId | int | FK → Employee |
| Month | int | Tháng |
| TotalAmount | decimal(18,2) | Tổng lương |
| PaidAmount | decimal(18,2) | Đã thanh toán |
| StatusId | int | FK → CommonCode (trạng thái) |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime? | Ngày cập nhật |

---

## 3. API Endpoints

### Base URL: `/api/SalaryInfo`

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| `GET` | `/` | Lấy danh sách thông tin lương |
| `GET` | `/{id}` | Lấy chi tiết theo ID |
| `POST` | `/` | Tạo mới thông tin lương |
| `PUT` | `/{id}` | Cập nhật thông tin lương |
| `DELETE` | `/{id}` | Xóa thông tin lương |
| `POST` | `/calculate` | **Tính lương tháng & xuất Excel** |

---

## 4. Request/Response Models

### 4.1 GetSalaryInfoRequest

```json
{
    "employeeId": 1,        // Optional - Filter theo nhân viên
    "year": 2025,           // Optional - Filter theo năm
    "pageIndex": 1,
    "pageSize": 10
}
```

### 4.2 PostSalaryInfoRequest

```json
{
    "employeeId": 1,        // Required
    "year": 2025,           // Required
    "baseSalary": 15000000, // Required - Lương cơ bản
    "yearBonus": 3000000,   // Optional - Thưởng năm
    "allowance": 2000000    // Optional - Phụ cấp
}
```

### 4.3 CalculateSalaryRequest

```json
{
    "employeeId": 1,              // Required
    "year": 2025,                 // Optional - Mặc định: năm hiện tại
    "month": 12,                  // Optional - Mặc định: tháng hiện tại
    "standardMonthlyHours": 208,  // Optional - Số giờ chuẩn/tháng (mặc định: 208)
    "overtimeMultiplier": 1.5     // Optional - Hệ số OT (mặc định: 1.5)
}
```

### 4.4 SalaryInfoDto (Response)

```json
{
    "salaryInfoId": 1,
    "employeeId": 1,
    "year": 2025,
    "baseSalary": 15000000,
    "yearBonus": 3000000,
    "allowance": 2000000,
    "createdAt": "2025-01-01T00:00:00Z",
    "updatedAt": "2025-06-15T10:30:00Z"
}
```

### 4.5 SalaryCalculationDto

```json
{
    "employeeId": 1,
    "year": 2025,
    "month": 12,
    "totalWorkHours": 176,
    "totalOvertimeHours": 24,
    "baseSalary": 15000000,
    "hourlyRate": 72115.38,
    "basePay": 12692307.69,
    "overtimePay": 2596153.85,
    "totalPay": 15288461.54
}
```

---

## 5. Công thức tính lương

### 5.1 Các tham số

| Tham số | Giá trị mặc định | Mô tả |
|---------|------------------|-------|
| StandardMonthlyHours | 208 giờ | Số giờ làm việc chuẩn/tháng (26 ngày × 8 giờ) |
| OvertimeMultiplier | 1.5x | Hệ số lương làm thêm giờ |

### 5.2 Công thức

```
1. HourlyRate = BaseSalary / StandardMonthlyHours

2. Từ Attendance records mỗi ngày:
   - NormalHours = min(8h, worked_hours)
   - OvertimeHours = max(0, worked_hours - 8h)

3. BasePay = BaseSalary × min(1, TotalNormalHours / StandardMonthlyHours)

4. OvertimePay = TotalOvertimeHours × HourlyRate × OvertimeMultiplier

5. TotalPay = BasePay + OvertimePay
```

### 5.3 Ví dụ tính toán

```
Input:
- BaseSalary = 15,000,000 VND
- StandardMonthlyHours = 208 giờ
- TotalNormalHours = 176 giờ (22 ngày × 8 giờ)
- TotalOvertimeHours = 24 giờ
- OvertimeMultiplier = 1.5

Tính toán:
- HourlyRate = 15,000,000 / 208 = 72,115.38 VND/giờ
- BasePay = 15,000,000 × (176 / 208) = 12,692,307.69 VND
- OvertimePay = 24 × 72,115.38 × 1.5 = 2,596,153.85 VND
- TotalPay = 12,692,307.69 + 2,596,153.85 = 15,288,461.54 VND
```

---

## 6. Xử lý đặc biệt

### 6.1 Ca đêm (Overnight shift)

```csharp
if (checkOutTime < checkInTime) {
    // Ca đêm: checkout < checkin (qua ngày hôm sau)
    // Ví dụ: checkin 22:00, checkout 06:00
    duration = (checkOutTime + 24h) - checkInTime;
}
```

### 6.2 Ngày nghỉ phép / Vắng mặt

| Trạng thái | Mô tả |
|------------|-------|
| `AbsentWithLeave` | Nghỉ có phép (SickDays) |
| `AbsentWithoutLeave` | Nghỉ không phép (AbsentDays) |

---

## 7. Output: Excel Salary Statement

Khi gọi API `/calculate`, hệ thống trả về file Excel với các thông tin:

| Nội dung | Mô tả |
|----------|-------|
| Employee ID | Mã nhân viên |
| Employee Name | Tên nhân viên |
| Year / Month | Năm / Tháng tính lương |
| Base Salary | Lương cơ bản |
| Total Normal Hours | Tổng giờ làm việc thường |
| Total Overtime Hours | Tổng giờ làm thêm |
| Sick Days | Số ngày nghỉ ốm |
| Absent Days | Số ngày vắng mặt |
| Base Pay | Lương cơ bản thực nhận |
| Overtime Pay | Lương làm thêm giờ |
| **Total Pay** | **Tổng lương** |

---

## 8. Flow tổng quan

```
┌─────────────────────┐
│     SalaryInfo      │
│  (Lương cơ bản/năm) │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│     Attendance      │
│  (Chấm công/ngày)   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  CalculateSalary    │
│  (Tính lương tháng) │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│    Excel Export     │
│  (Xuất bảng lương)  │
└─────────────────────┘
```

---

## 9. Ví dụ sử dụng API

### 9.1 Tạo thông tin lương cho nhân viên

**Request:**
```http
POST /api/SalaryInfo
Content-Type: application/json

{
    "employeeId": 1,
    "year": 2025,
    "baseSalary": 15000000,
    "yearBonus": 3000000,
    "allowance": 2000000
}
```

**Response:**
```json
{
    "isSuccess": true,
    "responseCode": "SUCCESS",
    "message": "Create SalaryInfo successful",
    "data": {
        "salaryInfoId": 1,
        "employeeId": 1,
        "year": 2025,
        "baseSalary": 15000000,
        "yearBonus": 3000000,
        "allowance": 2000000,
        "createdAt": "2025-12-22T10:00:00Z"
    },
    "statusCode": 201
}
```

### 9.2 Tính lương tháng và xuất Excel

**Request:**
```http
POST /api/SalaryInfo/calculate
Content-Type: application/json

{
    "employeeId": 1,
    "year": 2025,
    "month": 12
}
```

**Response:** File Excel download (`salary_1_2025_12.xlsx`)

---

## 10. Dependencies

| Package | Mục đích |
|---------|----------|
| ClosedXML | Xuất file Excel |
| AutoMapper | Map Entity ↔ DTO |

---

## 11. Lưu ý quan trọng

1. **SalaryInfo theo năm**: Mỗi nhân viên có thể có nhiều SalaryInfo cho các năm khác nhau
2. **Fallback BaseSalary**: Nếu không có SalaryInfo cho năm yêu cầu, sử dụng `Employee.BaseSalary`
3. **Attendance bắt buộc**: Phải có dữ liệu chấm công để tính lương chính xác
4. **Ca đêm**: Hệ thống tự động xử lý ca làm việc qua đêm (checkout < checkin)
5. **Làm tròn**: Tất cả số tiền được làm tròn 2 chữ số thập phân

---

*Cập nhật lần cuối: 22/12/2025*

