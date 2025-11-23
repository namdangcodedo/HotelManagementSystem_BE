# Task: Dashboard API Development

## Module Overview
**Dashboard Module** cung cấp các API để hiển thị trang tổng quan cho Admin và Manager, bao gồm:
- Thống kê tổng quan (doanh thu, số booking, khách hàng)
- Biểu đồ theo thời gian (doanh thu theo tháng, số booking)
- Top danh sách (phòng hot, khách hàng VIP)
- Hoạt động gần đây
- Báo cáo nhanh

## Business Requirements
- Hiển thị dữ liệu real-time hoặc gần real-time
- Filter theo khoảng thời gian (ngày, tuần, tháng, năm)
- Phân quyền theo role (Admin xem tất cả, Manager xem theo khu vực)
- Performance tối ưu (cache dữ liệu)
- Export báo cáo (PDF/Excel)

## Required APIs Analysis

### 1. Overview Statistics APIs
#### Get Dashboard Overview
- **Endpoint:** `GET /api/dashboard/overview`
- **Purpose:** Lấy thống kê tổng quan cho dashboard
- **Query Params:** fromDate, toDate
- **Response:**
  ```json
  {
    "totalRevenue": 150000000,
    "totalBookings": 1250,
    "totalCustomers": 850,
    "occupancyRate": 85.5,
    "averageBookingValue": 120000
  }
  ```

#### Get Revenue Statistics
- **Endpoint:** `GET /api/dashboard/revenue`
- **Purpose:** Thống kê doanh thu theo thời gian
- **Query Params:** fromDate, toDate, groupBy (day/week/month)
- **Response:** Array of revenue data points

#### Get Booking Statistics
- **Endpoint:** `GET /api/dashboard/bookings`
- **Purpose:** Thống kê số booking theo thời gian
- **Query Params:** fromDate, toDate, groupBy, bookingType (Online/Walkin)
- **Response:** Array of booking count data points

### 2. Top Lists APIs
#### Get Top Rooms
- **Endpoint:** `GET /api/dashboard/top-rooms`
- **Purpose:** Danh sách phòng được đặt nhiều nhất
- **Query Params:** fromDate, toDate, limit (default: 10)
- **Response:** Array of rooms with booking count and revenue

#### Get Top Customers
- **Endpoint:** `GET /api/dashboard/top-customers`
- **Purpose:** Danh sách khách hàng chi tiêu nhiều nhất
- **Query Params:** fromDate, toDate, limit
- **Response:** Array of customers with total spent and booking count

#### Get Top Room Types
- **Endpoint:** `GET /api/dashboard/top-room-types`
- **Purpose:** Loại phòng phổ biến nhất
- **Query Params:** fromDate, toDate, limit
- **Response:** Array of room types with statistics

### 3. Recent Activities APIs
#### Get Recent Bookings
- **Endpoint:** `GET /api/dashboard/recent-bookings`
- **Purpose:** Danh sách booking gần đây
- **Query Params:** limit (default: 20)
- **Response:** Array of recent bookings with customer info

#### Get Recent Payments
- **Endpoint:** `GET /api/dashboard/recent-payments`
- **Purpose:** Danh sách thanh toán gần đây
- **Query Params:** limit
- **Response:** Array of recent payments

#### Get System Alerts
- **Endpoint:** `GET /api/dashboard/alerts`
- **Purpose:** Cảnh báo hệ thống (phòng trống, booking sắp đến hạn)
- **Response:** Array of alerts

### 4. Detailed Reports APIs
#### Get Revenue Report
- **Endpoint:** `GET /api/dashboard/reports/revenue`
- **Purpose:** Báo cáo doanh thu chi tiết
- **Query Params:** fromDate, toDate, format (json/pdf/excel)
- **Response:** Detailed revenue report

#### Get Occupancy Report
- **Endpoint:** `GET /api/dashboard/reports/occupancy`
- **Purpose:** Báo cáo tỷ lệ lấp đầy phòng
- **Query Params:** fromDate, toDate, format
- **Response:** Occupancy report by date/room

#### Get Customer Report
- **Endpoint:** `GET /api/dashboard/reports/customers`
- **Purpose:** Báo cáo khách hàng
- **Query Params:** fromDate, toDate, format
- **Response:** Customer statistics and demographics

### 5. Real-time Data APIs
#### Get Live Occupancy
- **Endpoint:** `GET /api/dashboard/live/occupancy`
- **Purpose:** Tỷ lệ lấp đầy phòng hiện tại
- **Response:** Current occupancy percentage

#### Get Today's Bookings
- **Endpoint:** `GET /api/dashboard/live/today-bookings`
- **Purpose:** Số booking hôm nay
- **Response:** Today's booking count and revenue

#### Get Pending Tasks
- **Endpoint:** `GET /api/dashboard/live/pending-tasks`
- **Purpose:** Nhiệm vụ đang chờ (check-in, check-out, cleaning)
- **Response:** Count of pending tasks by type

## Database Schema Requirements
- Sử dụng existing tables: Booking, Payment, User, Room
- Thêm cache tables nếu cần: DashboardCache, ReportCache
- Views cho performance: RevenueByMonth, TopCustomers

## Security Considerations
- Authentication required for all APIs
- Role-based access: Admin (full access), Manager (limited)
- Audit logging for report exports
- Rate limiting for real-time APIs

## Implementation Steps
1. Tạo Models: DashboardStats, RevenueData, TopItem
2. Tạo DTOs cho request/response
3. Implement Services: DashboardService, ReportService
4. Tạo Controllers với endpoints trên
5. Implement caching (Redis/Memory)
6. Add background jobs for data aggregation
7. Unit tests và integration tests

## Dependencies
- EF Core for data access
- AutoMapper for DTO mapping
- FluentValidation for request validation
- Cache library (Microsoft.Extensions.Caching)
- Report generation library (for PDF/Excel export)

## Performance Optimization
- Cache dashboard data for 5-15 minutes
- Use database indexes on date columns
- Implement pagination for large datasets
- Background processing for heavy reports

---
Task này định nghĩa đầy đủ các API cần thiết để xây dựng dashboard tổng quan cho hệ thống khách sạn.
