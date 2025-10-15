# API Tests Documentation

This folder contains HTTP request files for testing the Hotel Management System APIs.

## 📁 Test Files Overview

| File | Description | Endpoints Covered |
|------|-------------|-------------------|
| `test-account-summary-api.http` | Account summary and profile management | View/Update account info, Admin/Manager operations |
| `test-amenity-api.http` | Amenity management | CRUD operations for hotel amenities |
| `test-commoncode-api.http` | Common code management | Employee types, room types, status codes |
| `test-employee-api.http` | Employee management | CRUD operations for employees |
| `test-room-api.http` | Room and room type management | Room types and room CRUD operations |

## 🚀 How to Use

### Prerequisites
- Make sure the API server is running (default: `http://localhost:8080`)
- Install a REST client that supports `.http` files (e.g., JetBrains IDEs, VS Code with REST Client extension)

### Authentication
Most test files include authentication steps:
1. Login as Admin: `admin@hotel.com` / `Admin@123`
2. Login as Manager: `manager@hotel.com` / `Manager@123`
3. The token is automatically saved to global variables for subsequent requests

### Running Tests
1. Open any `.http` file in your IDE
2. Execute requests sequentially or individually
3. Tokens are automatically captured and reused

## 🔑 Default Test Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@hotel.com | Admin@123 |
| Manager | manager@hotel.com | Manager@123 |

## 📝 Variables

All test files use the following base configuration:
- `@baseUrl = http://localhost:8080/api`
- Authentication tokens are stored in global variables: `admin_token`, `manager_token`, `auth_token`

## 🧪 Test Coverage

### Account Summary API
- ✅ View own account summary
- ✅ Update own account information
- ✅ Admin/Manager view any account
- ✅ Admin/Manager update any account

### Amenity API
- ✅ Public endpoints (no auth required)
- ✅ CRUD operations (Admin/Manager)
- ✅ Pagination and search
- ✅ Filter by active status

### Common Code API
- ✅ Get all code types
- ✅ Get codes by type
- ✅ Pagination and filtering
- ✅ Search functionality

### Employee API
- ✅ Create new employee
- ✅ Get employee list with pagination
- ✅ Get employee details
- ✅ Update employee information
- ✅ Delete employee
- ✅ Filter by employee type and status

### Room API
- ✅ Room type management
- ✅ Room CRUD operations
- ✅ Public and authenticated endpoints
- ✅ Search and filter capabilities

## 📌 Notes

- Some endpoints are public (no authentication required)
- Admin and Manager roles have different permissions
- All requests support pagination with `PageIndex` and `PageSize` parameters
- Search functionality is available on most list endpoints
- Remember to update `@baseUrl` if your API runs on a different port

## 🔄 Update History

- **2025-10-15**: Organized all API test files into dedicated folder structure

