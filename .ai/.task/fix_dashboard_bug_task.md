# Task: Fix Dashboard Services Bug - Schema Issues

## Problem Description
**Dashboard Services** đang gặp lỗi "tùm lum" liên quan đến schema không hiểu. Các triệu chứng:
- Lỗi khi query database cho thống kê dashboard
- Schema mismatch giữa models và database tables
- CommonCode references không đúng
- Null reference exceptions khi fetch data
- Performance issues do queries không tối ưu

## Root Cause Analysis
1. **Schema Mismatch:** Models không sync với database schema
2. **CommonCode Issues:** Sử dụng hardcode IDs thay vì CodeName
3. **Query Optimization:** Queries phức tạp gây timeout
4. **Null Handling:** Không handle null values properly
5. **Date Filtering:** Logic filter ngày tháng sai

## Required Fixes

### 1. Schema Validation & Sync
#### Check Database Schema
- Verify all tables exist: Booking, Payment, User, Room, CommonCode
- Check foreign key relationships
- Validate column types and constraints
- Ensure indexes on frequently queried columns (CreatedAt, StatusId)

#### Update Models
- Sync EF models with database schema
- Add missing navigation properties
- Update data annotations
- Fix enum mappings

### 2. CommonCode Integration
#### Replace Hardcoded IDs
```csharp
// BEFORE (WRONG)
var paidStatus = await _context.Payments.Where(p => p.StatusId == 1).ToListAsync();

// AFTER (CORRECT)
var paidStatusCode = await _unitOfWork.CommonCodes
    .FirstOrDefaultAsync(c => c.CodeType == "PaymentStatus" && c.CodeName == "Paid");
var paidStatus = await _context.Payments.Where(p => p.StatusId == paidStatusCode?.CodeId).ToListAsync();
```

#### Create Helper Methods
- `GetCommonCodeId(string codeType, string codeName)`
- `GetCommonCodeByType(string codeType)`
- Cache CommonCode data to avoid repeated queries

### 3. Query Optimization
#### Fix Dashboard Overview Query
```csharp
// BEFORE (INEFFICIENT)
var overview = await _context.Bookings
    .Include(b => b.Payments)
    .Where(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate)
    .GroupBy(b => 1)
    .Select(g => new {
        TotalRevenue = g.Sum(b => b.Payments.Sum(p => p.Amount)),
        TotalBookings = g.Count()
    }).FirstOrDefaultAsync();

// AFTER (OPTIMIZED)
var totalRevenue = await _context.Payments
    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate && p.StatusId == paidStatusId)
    .SumAsync(p => p.Amount);

var totalBookings = await _context.Bookings
    .CountAsync(b => b.CreatedAt >= fromDate && b.CreatedAt <= toDate);
```

#### Implement Caching
- Cache dashboard stats for 5-10 minutes
- Use Redis or Memory cache
- Invalidate cache on data changes

### 4. Null Safety & Error Handling
#### Add Null Checks
```csharp
var revenue = await _context.Payments
    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
    .SumAsync(p => p.Amount ?? 0);

var customerCount = await _context.Users
    .CountAsync(u => u.CreatedAt >= fromDate && u.CreatedAt <= toDate && u.RoleId != null);
```

#### Global Exception Handling
- Wrap service methods in try-catch
- Log errors with context
- Return meaningful error messages

### 5. Date Filtering Logic
#### Fix Date Range Queries
```csharp
// Ensure inclusive date ranges
var fromDate = request.FromDate?.Date ?? DateTime.Today.AddMonths(-1);
var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

// For monthly grouping
var monthlyStats = await _context.Payments
    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
    .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
    .Select(g => new {
        Year = g.Key.Year,
        Month = g.Key.Month,
        Revenue = g.Sum(p => p.Amount)
    })
    .OrderBy(x => x.Year).ThenBy(x => x.Month)
    .ToListAsync();
```

### 6. Top Lists Optimization
#### Fix Top Rooms Query
```csharp
var topRooms = await _context.BookingRooms
    .Where(br => br.Booking.CreatedAt >= fromDate && br.Booking.CreatedAt <= toDate)
    .GroupBy(br => br.RoomId)
    .Select(g => new {
        RoomId = g.Key,
        BookingCount = g.Count(),
        Revenue = g.Sum(br => br.Booking.TotalAmount)
    })
    .OrderByDescending(x => x.BookingCount)
    .Take(limit)
    .Join(_context.Rooms, x => x.RoomId, r => r.RoomId, (x, r) => new {
        r.RoomNumber,
        x.BookingCount,
        x.Revenue
    })
    .ToListAsync();
```

### 7. Testing & Validation
#### Unit Tests
- Test each service method with mock data
- Test edge cases (empty data, null dates)
- Test CommonCode integration

#### Integration Tests
- Test with real database
- Test performance with large datasets
- Test caching behavior

## Implementation Steps
1. **Audit Current Code:** Review all dashboard service methods for issues
2. **Schema Validation:** Run database schema comparison
3. **Fix CommonCode Usage:** Replace all hardcoded IDs
4. **Optimize Queries:** Rewrite inefficient queries
5. **Add Error Handling:** Implement try-catch and null checks
6. **Implement Caching:** Add caching layer
7. **Testing:** Write and run tests
8. **Performance Monitoring:** Add logging and metrics

## Expected Outcomes
- Dashboard loads without errors
- Data displays correctly
- Improved performance (response time < 2s)
- Accurate statistics
- Proper error messages for edge cases

## Dependencies
- EF Core tools for schema validation
- NUnit/xUnit for testing
- Serilog for logging
- Redis for caching (optional)

---
Task này tập trung fix các bug schema và logic trong Dashboard Services để đảm bảo hoạt động ổn định.
