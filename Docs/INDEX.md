# 📚 Hotel Management System - API Documentation Index

> **Purpose**: Tài liệu tổng hợp cho AI Code Generation & Development Reference

**Last Updated**: October 15, 2025

---

## 📖 Documentation Overview

Folder này chứa toàn bộ tài liệu API và kiến trúc hệ thống Hotel Management System, được tổ chức để hỗ trợ AI trong việc:
- Hiểu rõ cấu trúc dự án
- Generate code theo đúng pattern đã định sẵn
- Maintain consistency across codebase
- Quick reference cho các API endpoints

---

## 📑 Table of Contents

### 1️⃣ Project Architecture & Setup
**File**: [`PROJECT_ARCHITECTURE.md`](./PROJECT_ARCHITECTURE.md)

**Content**:
- 🏗️ Layer Responsibilities (API, Business, Repository, Services)
- 🔄 Code Flow Examples
- 📦 Project Structure
- 🛠️ Technology Stack
- ⚙️ Setup & Configuration

**Use Cases**:
- Hiểu tổng quan kiến trúc dự án
- Biết cách tổ chức code theo layers
- Pattern để tạo mới Controller, Service, Repository

---

### 2️⃣ API Refactoring Summary
**File**: [`API_REFACTORING_SUMMARY.md`](./API_REFACTORING_SUMMARY.md)

**Content**:
- ✨ BaseApiController features
- 🎯 Standardized Response Handling
- 🔐 User Context Properties
- 📊 Before/After refactoring examples
- ✅ Best Practices

**Use Cases**:
- Cách viết Controller theo chuẩn mới
- Sử dụng BaseApiController
- Handle responses consistently
- Check user permissions (IsAdmin, IsManager, HasRole)

---

### 3️⃣ Employee Role Mapping
**File**: [`EMPLOYEE_ROLE_MAPPING.md`](./EMPLOYEE_ROLE_MAPPING.md)

**Content**:
- 🔗 Mapping giữa EmployeeType và Role
- 📊 Bảng đối chiếu chi tiết
- 🎯 Quy tắc đặt tên
- ⚙️ Cơ chế tự động mapping

**Use Cases**:
- Hiểu cách hệ thống map giữa CommonCode.EmployeeType và Role
- Đảm bảo consistency khi thêm employee type mới
- Reference cho authorization logic

---

### 4️⃣ Account Summary API Documentation
**File**: [`ACCOUNT_SUMMARY_API_DOCUMENTATION.md`](./ACCOUNT_SUMMARY_API_DOCUMENTATION.md)

**Content**:
- 🔐 Phân quyền chi tiết
- 📋 Endpoint specifications
- 💾 Response structure
- 🎯 Business rules
- ⚠️ Error handling

**Use Cases**:
- Reference khi implement account-related features
- Hiểu cách phân quyền xem/sửa thông tin account
- Pattern cho việc hiển thị statistics (chỉ Admin)

---

### 5️⃣ API Tests Documentation
**File**: [`API_TESTS.md`](./API_TESTS.md)

**Content**:
- 🧪 Test files overview
- 🔑 Default test accounts
- 📝 Test variables configuration
- ✅ Test coverage details
- 🚀 How to run tests

**Use Cases**:
- Quick reference cho test credentials
- Hiểu endpoints nào đã được implement
- Pattern để viết test cases mới
- Verify API functionality

---

## 🎯 Quick Reference

### Standard Response Format
```csharp
public class ResultModel<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
}
```

### Base Controller Usage
```csharp
public class MyController : BaseApiController
{
    // Access current user info
    var userId = CurrentUserId;
    var isAdmin = IsAdmin;
    
    // Handle service results
    var result = await _service.GetData();
    return HandleResult(result);
}
```

### Authentication Roles
- **Admin**: Full system access + statistics
- **Manager**: Management operations
- **Receptionist**: Front desk operations
- **Housekeeper**: Room cleaning
- **Technician**: Maintenance
- **Security**: Security operations
- **Chef**: Kitchen operations
- **Waiter**: Service operations
- **Customer**: Guest user

### Default Test Credentials
```
Admin:    admin@hotel.com / Admin@123
Manager:  manager@hotel.com / Manager@123
```

### API Base URL
```
Development: http://localhost:8080/api
Production:  [TBD]
```

---

## 🤖 AI Code Generation Guidelines

### When Creating New Features:

1. **Check Architecture** → Read `PROJECT_ARCHITECTURE.md` for layer responsibilities
2. **Follow Patterns** → Use `API_REFACTORING_SUMMARY.md` for controller patterns
3. **Check Roles** → Reference `EMPLOYEE_ROLE_MAPPING.md` for authorization
4. **Verify APIs** → Check `API_TESTS.md` to avoid duplication

### Coding Standards:

✅ **DO**:
- Inherit from `BaseApiController` for all controllers
- Use `HandleResult()` for consistent responses
- Check permissions with `IsAdmin`, `IsManager`, `HasRole()`
- Follow the 4-layer architecture (API → Service → Repository → Data)
- Use DTOs for API responses (never expose entities directly)

❌ **DON'T**:
- Write custom response handling in controllers
- Expose sensitive data (passwords, internal IDs)
- Mix business logic in controllers
- Direct database access from controllers

### File Naming Conventions:
```
Controllers:  {Feature}Controller.cs
Services:     I{Feature}Service.cs, {Feature}Service.cs
Repositories: I{Feature}Repository.cs, {Feature}Repository.cs
DTOs:         {Feature}Dto.cs, {Feature}RequestDto.cs, {Feature}ResponseDto.cs
```

---

## 📁 Related Resources

- **API Test Files**: `../AppBackend.ApiCore/ApiTests/*.http`
- **Controllers**: `../AppBackend.ApiCore/Controllers/`
- **Services**: `../AppBackend.Services/Services/`
- **Repositories**: `../AppBackend.Repositories/Repositories/`
- **Models & DTOs**: `../AppBackend.BusinessObjects/`

---

## 🔄 Update History

| Date | Changes | Updated By |
|------|---------|------------|
| 2025-10-15 | Created documentation index and organized all docs | System |
| 2025-10-15 | Added API tests documentation | System |
| 2025-10-15 | Added project architecture guide | System |

---

## 💡 Tips for AI

- Always read `INDEX.md` first to understand available documentation
- Reference specific docs based on the task (e.g., creating controller → read API_REFACTORING_SUMMARY.md)
- Follow established patterns to maintain consistency
- Check test files to understand expected behavior
- Use the architecture guide to understand data flow

---

**Happy Coding! 🚀**

