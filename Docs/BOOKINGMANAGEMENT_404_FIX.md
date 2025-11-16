# BookingManagement API 404 Error - Root Cause and Fix

## Date: November 16, 2025

## Problem Summary
All BookingManagement API endpoints were returning **404 Not Found** errors during testing.

## Root Cause
The `BookingManagementController` was **missing the `[Route("api/[controller]")]` attribute**.

Without this attribute, ASP.NET Core could not map the HTTP requests to the controller endpoints, resulting in 404 errors for all routes like:
- `/api/BookingManagement/search-customer`
- `/api/BookingManagement/available-rooms`
- `/api/BookingManagement/offline-booking`
- etc.

## Evidence from Test Results
```
GET http://localhost:8080/api/BookingManagement/search-customer?searchTerm=nguyenvana@gmail.com
HTTP/1.1 404 Not Found
Time: 10ms

POST http://localhost:8080/api/BookingManagement/offline-booking
HTTP/1.1 404 Not Found
Time: 4ms
```

All requests returned 404 within 2-10ms, indicating the routes were not registered (not a business logic error).

## Fix Applied
Added the missing `[Route("api/[controller]")]` attribute to the controller:

```csharp
namespace AppBackend.ApiCore.Controllers
{
    /// <summary>
    /// API quản lý booking offline - dành cho Lễ tân, Manager, Admin
    /// </summary>
    [Route("api/[controller]")]  // ← ADDED THIS LINE
    [Authorize(Roles = "Receptionist,Manager,Admin")]
    public class BookingManagementController : BaseApiController
    {
        // ...existing code...
    }
}
```

## Verification
The attribute is now consistent with all other controllers in the system:
- ✅ `BookingController` - has `[Route("api/[controller]")]`
- ✅ `AccountController` - has `[Route("api/[controller]")]`
- ✅ `AuthenticationController` - has `[Route("api/[controller]")]`
- ✅ `AmenityController` - has `[Route("api/[controller]")]`
- ✅ **BookingManagementController** - NOW has `[Route("api/[controller]")]` ✅

## Next Steps
1. **Restart the API server** if it's currently running
2. **Re-run the HTTP tests** in `test-booking-management-api.http`
3. All endpoints should now return proper responses (200, 400, 401, etc.) instead of 404

## Secondary Issue: Token Variables
The HTTP test file also had issues with token variable capture. The login requests use:
```http
# @name loginReceptionist
POST {{baseUrl}}/api/Authentication/login
...

@receptionistToken = {{loginReceptionist.response.body.data.token}}
```

This syntax is correct for IntelliJ/Rider HTTP client. The token should be captured from the login response structure:
```json
{
  "isSuccess": true,
  "message": "Đăng nhập thành công",
  "data": {
    "token": "eyJhbGc...",
    "refreshToken": "...",
    "roles": [...]
  }
}
```

**Note:** The token variables will work correctly once the HTTP client properly executes the named requests. The "Invalid request because of unsubstituted variable" errors were secondary issues that occurred because the tests couldn't proceed after the 404 errors.

## Conclusion
**This was a CODE issue, not a TEST issue.** The controller was missing a critical routing attribute that prevented ASP.NET Core from discovering and registering the endpoints.

