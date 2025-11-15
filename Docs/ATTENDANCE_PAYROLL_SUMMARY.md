# Schema Chấm Công và Tính Lương - Tóm Tắt

## 1. Database Schema

### Employee (đã có, thêm trường)
```sql
ALTER TABLE Employee ADD BaseSalary DECIMAL(18,2) NOT NULL DEFAULT 0;
```
- `BaseSalary`: Lương cơ bản hàng tháng

### Attendance (đã có, thêm trường)
```sql
ALTER TABLE Attendance ADD DeviceEmployeeId NVARCHAR(100) NULL;
CREATE INDEX IX_Attendance_DeviceEmployeeId ON Attendance(DeviceEmployeeId);
```
- `DeviceEmployeeId`: Mã nhân viên từ máy chấm công

### PayrollDisbursement (bảng mới)
```sql
CREATE TABLE PayrollDisbursement (
    PayrollDisbursementId INT PRIMARY KEY IDENTITY,
    EmployeeId INT NOT NULL,
    PayrollMonth INT NOT NULL,
    PayrollYear INT NOT NULL,
    BaseSalary DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    DisbursedAmount DECIMAL(18,2) NOT NULL,
    StatusId INT NOT NULL,
    DisbursedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy INT NULL,
    UpdatedAt DATETIME2 NULL,
    UpdatedBy INT NULL
);
```

## 2. API Endpoints

### Chấm Công
- `GET /api/attendance?employeeId=&month=&year=` - Lấy danh sách chấm công
- `POST /api/attendance` - Tạo/sync từ máy chấm công
- `PUT /api/attendance/{id}` - Sửa giờ vào/ra
- `GET /api/attendance/summary?employeeId=&month=&year=` - Tổng hợp tháng

### Tính Lương
- `POST /api/payroll/calculate` - Tính lương preview (chưa lưu)
- `POST /api/payroll/disbursement` - Chốt sổ lương tháng
- `POST /api/payroll/{id}/approve` - Duyệt lương
- `POST /api/payroll/{id}/disburse` - Đánh dấu đã giải ngân
- `GET /api/payroll?employeeId=&month=&year=` - Xem lịch sử lương

## 3. Công Thức Tính Lương

```javascript
// Constants
const STANDARD_MONTHLY_HOURS = 168; // 21 ngày * 8 giờ
const OT_MULTIPLIER = 1.5;
const NIGHT_MULTIPLIER = 1.3;

// Formulas
hourlyRate = baseSalary / STANDARD_MONTHLY_HOURS;
workedHours = (checkOut - checkIn) / 3600; // seconds to hours
normalHours = Math.min(workedHours, 8);
overtimeHours = Math.max(0, workedHours - 8);

normalPay = normalHours * hourlyRate;
overtimePay = overtimeHours * hourlyRate * OT_MULTIPLIER;
totalGrossPay = normalPay + overtimePay + allowances;
netPay = totalGrossPay - tax - insurance;
```

## 4. Response JSON Mẫu

### GET /api/attendance/summary
```json
{
  "employeeId": 45,
  "employeeName": "Nguyễn Văn A",
  "month": 11,
  "year": 2025,
  "baseSalary": 8000000.00,
  "hourlyRate": 47619.05,
  "totalWorkDays": 20,
  "totalWorkedHours": 176.5,
  "totalNormalHours": 160.0,
  "totalOvertimeHours": 16.5,
  "normalPay": 7619047.62,
  "overtimePay": 1178571.43,
  "totalGrossPay": 8797619.05,
  "taxDeduction": 879761.91,
  "insuranceDeduction": 640000.00,
  "netPay": 7277857.14
}
```

### GET /api/attendance?employeeId=45&month=11&year=2025
```json
{
  "data": [
    {
      "attendanceId": 123,
      "employeeId": 45,
      "deviceEmployeeId": "EMP00123",
      "checkIn": "2025-11-01T08:05:00",
      "checkOut": "2025-11-01T17:30:00",
      "shiftDate": "2025-11-01",
      "workedHours": 9.42,
      "normalHours": 8.00,
      "overtimeHours": 1.42,
      "lateMinutes": 5,
      "notes": null
    }
  ]
}
```

## 5. Quy Trình Tính Lương

1. **Sync dữ liệu** → Máy chấm công xuất data vào `Attendance`
2. **Verify** → HR kiểm tra, sửa lỗi (missing checkout, late...)
3. **Calculate** → System tính lương theo công thức
4. **Review** → HR/Manager xem và điều chỉnh
5. **Lock** → Tạo `PayrollDisbursement` (status: Pending)
6. **Approve** → Director duyệt (status: Approved)
7. **Disburse** → Kế toán chuyển tiền (status: Disbursed)

## 6. CommonCode Cần Thêm

```sql
-- PAYROLL_STATUS
INSERT INTO CommonCode (CodeType, CodeValue, CodeName) VALUES
('PAYROLL_STATUS', 'PENDING', 'Chờ duyệt'),
('PAYROLL_STATUS', 'APPROVED', 'Đã duyệt'),
('PAYROLL_STATUS', 'DISBURSED', 'Đã giải ngân');
```

## 7. UI Grid Columns

| Cột | Type | Ý Nghĩa |
|-----|------|---------|
| Nhân viên | string | Tên nhân viên |
| Mã máy | string | DeviceEmployeeId |
| Ngày | date | ShiftDate |
| Giờ vào | time | CheckIn |
| Giờ ra | time | CheckOut |
| Giờ làm | number | WorkedHours |
| Giờ thường | number | NormalHours (≤8) |
| Giờ OT | number | OvertimeHours (>8) |
| Đi muộn | number | LateMinutes |
| Ghi chú | string | Notes |
| Actions | buttons | Edit/Approve |

## 8. Quy Tắc Nghiệp Vụ

- **Đi muộn**: >15 phút = cảnh cáo
- **OT**: Hệ số 1.5 (ngày thường), 2.0 (cuối tuần), 3.0 (lễ)
- **Ca đêm**: 22:00-06:00, hệ số 1.3
- **Giờ chuẩn**: 168 giờ/tháng (có thể config)
- **Missing checkout**: Cần HR xử lý thủ công

## 9. Next Steps Implementation

1. Tạo migration cho `BaseSalary`, `DeviceEmployeeId`, `PayrollDisbursement`
2. Tạo DTO: `AttendanceSummaryDto`, `PayrollCalculationDto`
3. Tạo Service: `AttendanceService`, `PayrollService`
4. Implement API endpoints
5. Tạo Repository: `IPayrollRepository`
6. Unit tests cho công thức tính lương

