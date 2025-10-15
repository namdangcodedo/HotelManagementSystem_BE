# Hướng dẫn Mapping giữa EmployeeType và Role

## 📋 Cơ chế hoạt động

Hệ thống đã được cấu hình để **tự động mapping** giữa `CommonCode.EmployeeType` và `Role` thông qua:

```
CommonCode.CodeName (EmployeeType) === Role.RoleValue
CommonCode.CodeValue (EmployeeType) === Role.RoleName
```

## 🔗 Bảng Mapping - ĐỒNG BỘ HOÀN TOÀN

| EmployeeType (CodeValue) | EmployeeType (CodeName) | Role (RoleValue) | Role (RoleName) | Mapping Status |
|--------------------------|------------------------|------------------|-----------------|----------------|
| Quản trị viên | **Admin** | **Admin** | Quản trị viên | ✅ KHỚP |
| Quản lý | **Manager** | **Manager** | Quản lý | ✅ KHỚP |
| Lễ tân | **Receptionist** | **Receptionist** | Lễ tân | ✅ KHỚP |
| Nhân viên dọn phòng | **Housekeeper** | **Housekeeper** | Nhân viên dọn phòng | ✅ KHỚP |
| Kỹ thuật viên | **Technician** | **Technician** | Kỹ thuật viên | ✅ KHỚP |
| Bảo vệ | **Security** | **Security** | Bảo vệ | ✅ KHỚP |
| Đầu bếp | **Chef** | **Chef** | Đầu bếp | ✅ KHỚP |
| Nhân viên phục vụ | **Waiter** | **Waiter** | Nhân viên phục vụ | ✅ KHỚP |

### 📊 Quy tắc đặt tên

**Trong CommonCode (EmployeeType):**
- `CodeValue` = Tên tiếng Việt (hiển thị cho người dùng) = `Role.RoleName`
- `CodeName` = Tên tiếng Anh (dùng để mapping code) = `Role.RoleValue`

**Trong Role:**
- `RoleValue` = Tên tiếng Anh (dùng trong Authorization)
- `RoleName` = Tên tiếng Việt (hiển thị)

**Công thức mapping:**
```csharp
// Khi thêm Employee:
var employeeType = CommonCode.Find(id);
var role = Role.Find(r => r.RoleValue == employeeType.CodeName);
```

## 🎯 Cách sử dụng

### 1. Lấy danh sách EmployeeType

```http
GET /api/CommonCode/by-type/EmployeeType
```

Response:
```json
{
  "isSuccess": true,
  "data": [
    {
      "codeId": 12,
      "codeType": "EmployeeType",
      "codeValue": "Quản trị viên",
      "codeName": "Admin",
      "description": "Quản trị viên hệ thống",
      "displayOrder": 1,
      "isActive": true
    },
    ...
  ]
}
```

### 2. Thêm Employee mới

Khi thêm Employee, chỉ cần truyền `employeeTypeId` - hệ thống sẽ **tự động**:
1. Lấy `EmployeeType` từ `CommonCode`
2. Dùng `EmployeeType.CodeName` để tìm `Role.RoleValue` tương ứng
3. Gán Role cho Account của Employee

```http
POST /api/Employee
{
  "username": "nguyenvana",
  "email": "nguyenvana@hotel.com",
  "password": "Employee@123",
  "fullName": "Nguyễn Văn A",
  "phoneNumber": "0912345678",
  "employeeTypeId": 13,  // Manager EmployeeType
  "hireDate": "2024-01-15"
}
```

**Kết quả:** Employee được tạo với:
- EmployeeType = Manager (CodeId: 13)
- Role = Manager (tự động gán)

## 🔄 Flow hoạt động trong Code

```
AddEmployeeAsync()
  ↓
1. Lấy EmployeeType từ CommonCode theo employeeTypeId
  ↓
2. Kiểm tra employeeType.CodeType == "EmployeeType"
  ↓
3. Tạo Account & Employee
  ↓
4. Tìm Role dựa trên employeeType.CodeName
   → var role = await _unitOfWork.Roles.GetRoleByRoleValueAsync(employeeType.CodeName)
  ↓
5. Gán Role cho Account
   → AccountRole { AccountId, RoleId }
```

## 📝 Lưu ý khi thêm EmployeeType hoặc Role mới

**Quy tắc:** Khi thêm EmployeeType mới trong CommonCode, PHẢI có Role tương ứng với cùng RoleValue.

**Ví dụ:** Thêm "Kế toán"

1. Thêm Role trước:
```sql
INSERT INTO Role (RoleValue, RoleName, IsActive, CreatedAt)
VALUES ('Accountant', 'Kế toán', 1, GETDATE());
```

2. Thêm EmployeeType (CodeName phải khớp với RoleValue):
```sql
INSERT INTO CommonCode (CodeType, CodeValue, CodeName, Description, DisplayOrder, IsActive, CreatedAt)
VALUES ('EmployeeType', 'Kế toán', 'Accountant', 'Nhân viên kế toán', 9, 1, GETDATE());
```

Hoặc qua API:
```http
POST /api/CommonCode
{
  "codeType": "EmployeeType",
  "codeValue": "Kế toán",
  "codeName": "Accountant",  // PHẢI KHỚP với Role.RoleValue
  "description": "Nhân viên kế toán",
  "displayOrder": 9,
  "isActive": true
}
```

## ⚠️ Lỗi thường gặp

### Lỗi: Role không được gán tự động

**Nguyên nhân:** `EmployeeType.CodeName` không khớp với bất kỳ `Role.RoleValue` nào.

**Giải pháp:** 
- Kiểm tra chính tả của CodeName
- Đảm bảo Role đã tồn tại trong database
- CodeName phân biệt hoa thường

### Lỗi: EmployeeType không hợp lệ

**Nguyên nhân:** `employeeTypeId` không tồn tại hoặc không phải là EmployeeType.

**Giải pháp:**
- Lấy danh sách EmployeeType từ API trước
- Sử dụng CodeId từ danh sách đó

## 🚀 Testing

```bash
# 1. Lấy danh sách EmployeeType
GET /api/CommonCode/by-type/EmployeeType

# 2. Chọn một CodeId (ví dụ: 13 cho Manager)

# 3. Tạo Employee mới
POST /api/Employee
{
  "employeeTypeId": 13,  // Manager
  ...other fields...
}

# 4. Verify: Employee có Role = Manager
GET /api/Employee/{employeeId}
```

## 📊 Database Schema

```
CommonCode (EmployeeType)
├── CodeId: 12
├── CodeType: "EmployeeType"
├── CodeValue: "Quản trị viên"
├── CodeName: "Admin" ─────────┐
└── ...                        │
                               │ MATCH
Role                           │
├── RoleId: 1                  │
├── RoleValue: "Admin" ◄───────┘
├── RoleName: "Quản trị viên"
└── ...

Employee
├── EmployeeId
├── EmployeeTypeId: 12 (references CommonCode)
└── AccountId ──────┐
                    │
AccountRole         │
├── AccountId ◄─────┘
└── RoleId: 1 (Auto-assigned based on EmployeeType)
```
