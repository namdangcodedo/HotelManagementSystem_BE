# Employee Search API Documentation

## Overview
API endpoint for searching and filtering employees by various criteria including name, phone number, email, username, employee type, active status, and account lock status.

**Endpoint**: `GET /api/Employee/search`

**Authentication**: Required
**Roles**: `Admin`, `Manager`

---

## Endpoint Details

### HTTP Method
```
GET /api/Employee/search
```

### Authorization
- **Required**: Yes
- **Roles**: `Admin`, `Manager`
- **Header**: `Authorization: Bearer {token}`

---

## Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Keyword` | `string` | ❌ No | Từ khóa tìm kiếm (tìm trên: FullName, PhoneNumber, Email, Username, EmployeeType) |
| `EmployeeTypeId` | `int?` | ❌ No | Lọc theo loại nhân viên (1=Admin, 2=Manager, 3=Receptionist, etc.) |
| `IsActive` | `bool?` | ❌ No | Lọc theo trạng thái hoạt động (`true`=đang làm việc, `false`=đã nghỉ) |
| `IsLocked` | `bool?` | ❌ No | Lọc theo trạng thái tài khoản (`true`=bị khóa, `false`=không khóa) |
| `PageIndex` | `int` | ❌ No | Trang hiện tại (mặc định: 1) |
| `PageSize` | `int` | ❌ No | Số lượng bản ghi mỗi trang (mặc định: 10, tối đa: 100) |

**Note**: Tất cả các tham số đều không bắt buộc. Nếu không truyền tham số nào, API sẽ trả về tất cả nhân viên với phân trang mặc định.

---

## Request Examples

### Example 1: Tìm kiếm theo tên
```http
GET /api/Employee/search?keyword=nguyen&pageIndex=1&pageSize=10 HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 2: Lọc theo loại nhân viên và trạng thái
```http
GET /api/Employee/search?employeeTypeId=3&isActive=true HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 3: Tìm nhân viên bị khóa
```http
GET /api/Employee/search?isLocked=true&pageIndex=1&pageSize=20 HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 4: Tìm kiếm kết hợp nhiều điều kiện
```http
GET /api/Employee/search?keyword=manager&employeeTypeId=2&isActive=true&isLocked=false HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Example 5: Tìm tất cả nhân viên (không filter)
```http
GET /api/Employee/search?pageIndex=1&pageSize=50 HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Success Response (200 OK)

### Response Structure
```json
{
    "isSuccess": true,
    "responseCode": "SUCCESS",
    "statusCode": 200,
    "data": {
        "employees": [
            {
                "employeeId": 5,
                "fullName": "Nguyễn Văn A",
                "phoneNumber": "0987654321",
                "email": "nguyenvana@hotel.com",
                "dateOfBirth": "1990-05-15",
                "gender": "Nam",
                "address": "123 Đường ABC, Quận 1, TP.HCM",
                "identityCard": "079090001234",
                "employeeType": "Lễ tân",
                "employeeTypeId": 3,
                "employeeTypeCode": "Receptionist",
                "salary": 8000000.00,
                "hireDate": "2023-01-15",
                "isActive": true,
                "avatar": "https://res.cloudinary.com/hotel/image/upload/v123456789/avatars/employee_5.jpg",
                "account": {
                    "accountId": 12,
                    "username": "nguyenvana",
                    "email": "nguyenvana@hotel.com",
                    "isLocked": false,
                    "lastLoginAt": "2025-12-18T08:30:00Z",
                    "createdAt": "2023-01-15T10:00:00Z"
                },
                "createdAt": "2023-01-15T10:00:00Z",
                "updatedAt": "2025-12-01T14:20:00Z"
            },
            {
                "employeeId": 8,
                "fullName": "Trần Thị B",
                "phoneNumber": "0912345678",
                "email": "tranthib@hotel.com",
                "dateOfBirth": "1995-08-20",
                "gender": "Nữ",
                "address": "456 Đường XYZ, Quận 3, TP.HCM",
                "identityCard": "079095002345",
                "employeeType": "Nhân viên dọn phòng",
                "employeeTypeId": 4,
                "employeeTypeCode": "Housekeeper",
                "salary": 6000000.00,
                "hireDate": "2023-03-10",
                "isActive": true,
                "avatar": null,
                "account": {
                    "accountId": 15,
                    "username": "tranthib",
                    "email": "tranthib@hotel.com",
                    "isLocked": false,
                    "lastLoginAt": "2025-12-17T16:45:00Z",
                    "createdAt": "2023-03-10T09:00:00Z"
                },
                "createdAt": "2023-03-10T09:00:00Z",
                "updatedAt": "2025-11-15T11:30:00Z"
            }
        ],
        "pagination": {
            "totalRecords": 25,
            "totalPages": 3,
            "currentPage": 1,
            "pageSize": 10,
            "hasNextPage": true,
            "hasPreviousPage": false
        }
    },
    "message": "Tìm kiếm nhân viên thành công"
}
```

### Response Fields

#### Main Response
| Field | Type | Description |
|-------|------|-------------|
| `isSuccess` | `boolean` | Trạng thái thành công của request |
| `responseCode` | `string` | Mã response (SUCCESS, ERROR, etc.) |
| `statusCode` | `int` | HTTP status code |
| `message` | `string` | Thông báo kết quả |
| `data` | `object` | Dữ liệu trả về |

#### Employee Data
| Field | Type | Description |
|-------|------|-------------|
| `employees` | `array` | Danh sách nhân viên tìm được |
| `employees[].employeeId` | `int` | ID nhân viên |
| `employees[].fullName` | `string` | Họ tên đầy đủ |
| `employees[].phoneNumber` | `string?` | Số điện thoại |
| `employees[].email` | `string?` | Email |
| `employees[].dateOfBirth` | `date?` | Ngày sinh (YYYY-MM-DD) |
| `employees[].gender` | `string?` | Giới tính (Nam/Nữ) |
| `employees[].address` | `string?` | Địa chỉ |
| `employees[].identityCard` | `string?` | Số CMND/CCCD |
| `employees[].employeeType` | `string` | Tên loại nhân viên (Tiếng Việt) |
| `employees[].employeeTypeId` | `int` | ID loại nhân viên |
| `employees[].employeeTypeCode` | `string` | Mã loại nhân viên (English) |
| `employees[].salary` | `decimal?` | Lương cơ bản |
| `employees[].hireDate` | `date?` | Ngày vào làm |
| `employees[].isActive` | `boolean` | Trạng thái làm việc |
| `employees[].avatar` | `string?` | URL ảnh đại diện |
| `employees[].createdAt` | `datetime` | Thời gian tạo |
| `employees[].updatedAt` | `datetime?` | Thời gian cập nhật |

#### Account Information
| Field | Type | Description |
|-------|------|-------------|
| `account` | `object?` | Thông tin tài khoản (null nếu chưa có) |
| `account.accountId` | `int` | ID tài khoản |
| `account.username` | `string` | Tên đăng nhập |
| `account.email` | `string` | Email tài khoản |
| `account.isLocked` | `boolean` | Trạng thái khóa tài khoản |
| `account.lastLoginAt` | `datetime?` | Lần đăng nhập cuối |
| `account.createdAt` | `datetime` | Thời gian tạo tài khoản |

#### Pagination
| Field | Type | Description |
|-------|------|-------------|
| `pagination` | `object` | Thông tin phân trang |
| `pagination.totalRecords` | `int` | Tổng số bản ghi |
| `pagination.totalPages` | `int` | Tổng số trang |
| `pagination.currentPage` | `int` | Trang hiện tại |
| `pagination.pageSize` | `int` | Số bản ghi mỗi trang |
| `pagination.hasNextPage` | `boolean` | Có trang tiếp theo không |
| `pagination.hasPreviousPage` | `boolean` | Có trang trước không |

---

## Error Responses

### 400 Bad Request - Invalid Parameters
```json
{
    "isSuccess": false,
    "responseCode": "VALIDATION_ERROR",
    "statusCode": 400,
    "data": null,
    "message": "PageSize không được vượt quá 100"
}
```

**Common Validation Errors:**
- `PageIndex` phải lớn hơn 0
- `PageSize` phải từ 1 đến 100
- `Keyword` quá dài (tối đa 100 ký tự)

### 401 Unauthorized
```json
{
    "isSuccess": false,
    "responseCode": "UNAUTHORIZED",
    "statusCode": 401,
    "data": null,
    "message": "Token không hợp lệ hoặc đã hết hạn"
}
```

**Causes:**
- JWT token missing
- Token expired
- Invalid token format

**Solution**: Request a new token via login endpoint

### 403 Forbidden
```json
{
    "isSuccess": false,
    "responseCode": "FORBIDDEN",
    "statusCode": 403,
    "data": null,
    "message": "Bạn không có quyền truy cập tài nguyên này"
}
```

**Cause**: User role is not `Admin` or `Manager`

**Required Roles:**
- ✅ `Admin`
- ✅ `Manager`
- ❌ `Receptionist`, `Housekeeper`, etc.

### 500 Internal Server Error
```json
{
    "isSuccess": false,
    "responseCode": "SERVER_ERROR",
    "statusCode": 500,
    "data": null,
    "message": "Đã xảy ra lỗi: [Error details]"
}
```

---

## Search Behavior

### Keyword Search Fields
Khi sử dụng `keyword`, hệ thống sẽ tìm kiếm trên các trường sau:

1. **FullName** - Tên đầy đủ nhân viên
2. **PhoneNumber** - Số điện thoại
3. **Email** - Email
4. **Username** - Tên đăng nhập
5. **EmployeeType** - Loại nhân viên (cả tiếng Việt và English)

**Search Logic:**
- Case-insensitive (không phân biệt chữ hoa/thường)
- Partial match (tìm chuỗi con)
- Tìm kiếm trên TẤT CẢ các trường trên đồng thời

**Examples:**
- `keyword=nguyen` → Tìm "Nguyễn Văn A", "Trần Nguyễn B", "nguyen@email.com"
- `keyword=0987` → Tìm số điện thoại bắt đầu bằng 0987
- `keyword=manager` → Tìm nhân viên có chức vụ Manager hoặc username chứa "manager"

### Filter Combinations

#### Multiple Filters (AND Logic)
Khi kết hợp nhiều filter, hệ thống sử dụng logic AND:

```
keyword=nguyen AND employeeTypeId=3 AND isActive=true
→ Tìm nhân viên Lễ tân có tên "Nguyen" và đang làm việc
```

#### Employee Type IDs
| ID | Type (Vietnamese) | Code (English) |
|----|-------------------|----------------|
| 1 | Quản trị viên | Admin |
| 2 | Quản lý | Manager |
| 3 | Lễ tân | Receptionist |
| 4 | Nhân viên dọn phòng | Housekeeper |
| 5 | Kỹ thuật viên | Technician |
| 6 | Bảo vệ | Security |
| 7 | Đầu bếp | Chef |
| 8 | Nhân viên phục vụ | Waiter |

---

## Use Cases

### Use Case 1: Tìm nhân viên để phân công ca làm việc
**Scenario**: Manager cần tìm Lễ tân đang làm việc để xếp lịch

**Request:**
```http
GET /api/Employee/search?employeeTypeId=3&isActive=true&isLocked=false
```

**Result**: Danh sách tất cả Lễ tân đang làm việc, tài khoản không bị khóa

---

### Use Case 2: Kiểm tra nhân viên bị khóa tài khoản
**Scenario**: Admin muốn xem danh sách nhân viên bị khóa

**Request:**
```http
GET /api/Employee/search?isLocked=true
```

**Result**: Danh sách nhân viên có tài khoản bị khóa

---

### Use Case 3: Tìm nhân viên theo số điện thoại
**Scenario**: Tìm nhanh nhân viên khi biết số điện thoại

**Request:**
```http
GET /api/Employee/search?keyword=0987654321
```

**Result**: Nhân viên có số điện thoại khớp

---

### Use Case 4: Danh sách nhân viên đã nghỉ việc
**Scenario**: HR cần xem danh sách nhân viên đã nghỉ

**Request:**
```http
GET /api/Employee/search?isActive=false&pageSize=50
```

**Result**: Tất cả nhân viên đã nghỉ việc

---

### Use Case 5: Tìm kiếm tổng hợp
**Scenario**: Tìm nhân viên dọn phòng có tên "Nguyen", đang làm việc

**Request:**
```http
GET /api/Employee/search?keyword=nguyen&employeeTypeId=4&isActive=true
```

**Result**: Nhân viên dọn phòng tên Nguyen đang làm việc

---

## Integration Examples

### TypeScript/JavaScript Example

```typescript
interface EmployeeSearchRequest {
  keyword?: string;
  employeeTypeId?: number;
  isActive?: boolean;
  isLocked?: boolean;
  pageIndex?: number;
  pageSize?: number;
}

interface EmployeeSearchResponse {
  isSuccess: boolean;
  responseCode: string;
  statusCode: number;
  data: {
    employees: Employee[];
    pagination: {
      totalRecords: number;
      totalPages: number;
      currentPage: number;
      pageSize: number;
      hasNextPage: boolean;
      hasPreviousPage: boolean;
    };
  };
  message: string;
}

interface Employee {
  employeeId: number;
  fullName: string;
  phoneNumber?: string;
  email?: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  identityCard?: string;
  employeeType: string;
  employeeTypeId: number;
  employeeTypeCode: string;
  salary?: number;
  hireDate?: string;
  isActive: boolean;
  avatar?: string;
  account?: {
    accountId: number;
    username: string;
    email: string;
    isLocked: boolean;
    lastLoginAt?: string;
    createdAt: string;
  };
  createdAt: string;
  updatedAt?: string;
}

// Service Class
class EmployeeService {
  private baseUrl = 'http://localhost:8080/api/Employee';
  private token = localStorage.getItem('token');

  async searchEmployees(request: EmployeeSearchRequest): Promise<EmployeeSearchResponse> {
    const params = new URLSearchParams();
    
    if (request.keyword) params.append('keyword', request.keyword);
    if (request.employeeTypeId) params.append('employeeTypeId', request.employeeTypeId.toString());
    if (request.isActive !== undefined) params.append('isActive', request.isActive.toString());
    if (request.isLocked !== undefined) params.append('isLocked', request.isLocked.toString());
    if (request.pageIndex) params.append('pageIndex', request.pageIndex.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());

    const response = await fetch(`${this.baseUrl}/search?${params}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      }
    });

    return await response.json();
  }
}

// Usage Examples
const employeeService = new EmployeeService();

// Example 1: Search by name
const result1 = await employeeService.searchEmployees({
  keyword: 'nguyen',
  pageIndex: 1,
  pageSize: 10
});

// Example 2: Get active receptionists
const result2 = await employeeService.searchEmployees({
  employeeTypeId: 3, // Receptionist
  isActive: true,
  isLocked: false
});

// Example 3: Get locked accounts
const result3 = await employeeService.searchEmployees({
  isLocked: true
});

// Example 4: Get all employees (paginated)
const result4 = await employeeService.searchEmployees({
  pageIndex: 1,
  pageSize: 50
});

console.log(`Found ${result1.data.employees.length} employees`);
console.log(`Total records: ${result1.data.pagination.totalRecords}`);
```

### React Hook Example

```typescript
import { useState, useEffect } from 'react';

function useEmployeeSearch(searchParams: EmployeeSearchRequest) {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [pagination, setPagination] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchEmployees = async () => {
      setLoading(true);
      setError(null);

      try {
        const employeeService = new EmployeeService();
        const result = await employeeService.searchEmployees(searchParams);

        if (result.isSuccess) {
          setEmployees(result.data.employees);
          setPagination(result.data.pagination);
        } else {
          setError(result.message);
        }
      } catch (err) {
        setError('Lỗi khi tìm kiếm nhân viên');
      } finally {
        setLoading(false);
      }
    };

    fetchEmployees();
  }, [searchParams]);

  return { employees, pagination, loading, error };
}

// Component usage
function EmployeeSearchPage() {
  const [searchKeyword, setSearchKeyword] = useState('');
  const [employeeType, setEmployeeType] = useState<number | undefined>();
  const [activeOnly, setActiveOnly] = useState(true);

  const { employees, pagination, loading, error } = useEmployeeSearch({
    keyword: searchKeyword,
    employeeTypeId: employeeType,
    isActive: activeOnly,
    pageIndex: 1,
    pageSize: 20
  });

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h1>Tìm kiếm nhân viên</h1>
      <input
        type="text"
        placeholder="Tìm theo tên, số điện thoại, email..."
        value={searchKeyword}
        onChange={(e) => setSearchKeyword(e.target.value)}
      />
      
      <select onChange={(e) => setEmployeeType(Number(e.target.value) || undefined)}>
        <option value="">Tất cả loại nhân viên</option>
        <option value="3">Lễ tân</option>
        <option value="4">Nhân viên dọn phòng</option>
        {/* ... more options */}
      </select>

      <label>
        <input
          type="checkbox"
          checked={activeOnly}
          onChange={(e) => setActiveOnly(e.target.checked)}
        />
        Chỉ nhân viên đang làm việc
      </label>

      <div>
        {employees.map(emp => (
          <div key={emp.employeeId}>
            <h3>{emp.fullName}</h3>
            <p>{emp.employeeType}</p>
            <p>{emp.phoneNumber}</p>
            <p>Status: {emp.isActive ? 'Đang làm việc' : 'Đã nghỉ'}</p>
          </div>
        ))}
      </div>

      {pagination && (
        <div>
          Page {pagination.currentPage} of {pagination.totalPages}
          (Total: {pagination.totalRecords} employees)
        </div>
      )}
    </div>
  );
}
```

### cURL Examples

```bash
# Example 1: Search by keyword
curl -X GET "http://localhost:8080/api/Employee/search?keyword=nguyen&pageIndex=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"

# Example 2: Get active receptionists
curl -X GET "http://localhost:8080/api/Employee/search?employeeTypeId=3&isActive=true&isLocked=false" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"

# Example 3: Get locked accounts
curl -X GET "http://localhost:8080/api/Employee/search?isLocked=true" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"

# Example 4: Complex search
curl -X GET "http://localhost:8080/api/Employee/search?keyword=manager&employeeTypeId=2&isActive=true&pageSize=50" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"

# Example 5: Get all employees
curl -X GET "http://localhost:8080/api/Employee/search?pageIndex=1&pageSize=100" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
```

---

## Performance Considerations

### Optimization Tips
1. **Use Specific Filters**: Narrow down results with `employeeTypeId` and `isActive` to reduce data
2. **Appropriate Page Size**: Use smaller `pageSize` (10-20) for UI lists, larger (50-100) for exports
3. **Index Fields**: The following fields are indexed for fast search:
   - `FullName`
   - `PhoneNumber`
   - `Email`
   - `EmployeeTypeId`
   - `IsActive`

### Typical Response Times
- **Small result set (< 10 employees)**: ~50-100ms
- **Medium result set (10-50 employees)**: ~100-200ms
- **Large result set (50-100 employees)**: ~200-400ms

---

## Testing Checklist

### Functional Tests
- [ ] Search by keyword returns matching employees
- [ ] Empty keyword returns all employees
- [ ] Filter by employeeTypeId works correctly
- [ ] Filter by isActive=true returns only active employees
- [ ] Filter by isLocked=true returns only locked accounts
- [ ] Pagination works correctly (first, middle, last page)
- [ ] PageSize limit enforced (max 100)
- [ ] Case-insensitive search works
- [ ] Partial match search works

### Authorization Tests
- [ ] Unauthorized request (no token) returns 401
- [ ] Invalid token returns 401
- [ ] Expired token returns 401
- [ ] Non-admin/manager role returns 403
- [ ] Admin role can access
- [ ] Manager role can access

### Edge Cases
- [ ] No results returns empty array (not error)
- [ ] Special characters in keyword handled correctly
- [ ] Very long keyword handled correctly
- [ ] Invalid employeeTypeId returns no results (not error)
- [ ] PageIndex beyond last page returns empty array

---

## Related APIs

- **GET /api/Employee/{employeeId}** - Get employee details
- **GET /api/Employee** - Get employee list (paginated, basic)
- **POST /api/Employee** - Add new employee
- **PUT /api/Employee/{employeeId}** - Update employee
- **PATCH /api/Employee/{employeeId}/ban** - Lock/unlock employee account

---

## Changelog

### Version 1.0 (2025-12-18)
- ✅ Initial API documentation
- ✅ Complete request/response examples
- ✅ All search filters documented
- ✅ Integration examples (TypeScript, React, cURL)
- ✅ Use cases and business scenarios
- ✅ Error handling complete
- ✅ Performance considerations documented

---

## Support

For issues or questions about the Employee Search API, please contact the development team or refer to the main Employee API documentation.

