# Tổng quan kiến trúc hệ thống Hotel Management Backend

> File này mô tả kiến trúc tổng thể của solution `AppBackend.sln` theo góc nhìn high‑level: các layer chính, trách nhiệm, luồng xử lý tiêu biểu và các tích hợp bên ngoài.

---

## 1. Kiến trúc tổng thể

Hệ thống backend được tổ chức theo mô hình **multi‑project, layered architecture**:

- `AppBackend.ApiCore` – **Presentation / API Layer**
- `AppBackend.BusinessObjects` – **Domain & Shared Kernel**
- `AppBackend.Repositories` – **Data Access Layer (DAL)**
- `AppBackend.Services` – **Business Logic Layer (BLL)**
- `AppBackend.Extensions` – **Infrastructure / Cross‑cutting configuration**
- `AppBackend.Tests` – **Automated tests**

Luồng phụ thuộc (dependency flow) đi theo một chiều:

`ApiCore → Services → Repositories → BusinessObjects`

Các layer **không** tham chiếu ngược lên trên để tránh vòng lặp phụ thuộc và giữ cho business logic độc lập với presentation.

Sơ đồ đơn giản:

```text
   ┌─────────────────────────┐
   │      ApiCore (API)      │  ← Controllers, Middlewares
   └────────────┬────────────┘
                │ calls
   ┌────────────▼────────────┐
   │   Services (BLL)        │  ← Booking, Transaction, Auth, ...
   └────────────┬────────────┘
                │ uses
   ┌────────────▼────────────┐
   │ Repositories (DAL)      │  ← UnitOfWork, Generic Repo, EF Core
   └────────────┬────────────┘
                │ depends on
   ┌────────────▼────────────┐
   │ BusinessObjects (Domain)│  ← Models, DTOs, Enums, Exceptions
   └─────────────────────────┘
```

Ngoài ra còn có các thành phần cross‑cutting:

- Cấu hình auth/JWT, Google Login, PayOS, VietQR, Email, Cache (Redis/In‑memory)
- Tài liệu nội bộ trong thư mục `Docs/` (Booking Flow, Room Availability, Account Activation, Google Login, Chatbot, ...)

---

## 2. Mô tả chi tiết từng project / layer

### 2.1. `AppBackend.ApiCore` – API Layer

**Vai trò chính**

- Cung cấp các REST API cho frontend (web SPA, mobile, admin portal).
- Xử lý HTTP: routing, model binding, validation, status code.
- Áp dụng middlewares: authentication/authorization, exception handling, logging, CORS, rate‑limiting...
- Khởi tạo DI container, load `appsettings.*.json`, cấu hình các service (GoogleAuth, Jwt, PayOS, Email, VietQR, ...).

**Các thành phần tiêu biểu**

- `Program.cs`
  - Tạo `WebApplication` host.
  - Đăng ký các service từ `AppBackend.Services` & `AppBackend.Repositories`.
  - Gắn cấu hình `GoogleAuthSettings`, `FrontendSettings`, `PayOS`, `VietQR`, `Jwt`, `EmailSettings`, ...
- `Controllers/`
  - `AuthenticationController` – login, logout, refresh token, reset password, Google login exchange, activation account.
  - `BookingController` – APIs cho luồng đặt phòng online/guest, kiểm tra phòng trống.
  - `TransactionController` – quản lý thanh toán, PayOS payment link, QR payment, deposit, refund.
  - Các controller khác (Room, Employee, Attendance/Payroll, v.v. – tùy theo schema trong BusinessObjects).
- `Middlewares/`
  - Xử lý lỗi chung, logging request/response, gắn correlation ID...
- `Settings/`
  - `FrontendSettings` – base URL FE, dùng để build redirect/callback URLs.

**Nguyên tắc**: Controller **rất mỏng**, chỉ nhận request → gọi **Service** tương ứng → trả `ResultModel`/DTO ra ngoài dưới dạng JSON.

---

### 2.2. `AppBackend.BusinessObjects` – Domain & Shared Kernel

**Vai trò chính**

- Đại diện cho **domain model** của hệ thống khách sạn.
- Chứa toàn bộ **entities**, **DTOs**, **enums**, **constants**, **exceptions** và các cấu trúc dùng chung.

**Thành phần tiêu biểu**

- `Models/`
  - Các entity ánh xạ trực tiếp tới DB: `Account`, `Customer`, `Booking`, `BookingRoom`, `Room`, `RoomType`, `Transaction`, `CommonCode`, `BankConfig`, `HolidayPricing`, `Role`, `AccountRole`, ...
- `Dtos/`
  - DTO cho API & Service: `BookingDto`, `TransactionDto`, `PaymentHistoryDto`, `BookingRoomDto`, `PayOSPaymentLinkDto`, `GoogleUserInfo`, `GoogleLoginResponse`, ...
- `Enums/`
  - `RoleEnums` (Admin, Manager, Receptionist, Housekeeper, Technician, User)
  - Các enum hỗ trợ logic (nếu có bổ sung: BookingStatus, PaymentStatus, ...)
- `Constants/`
  - Chuỗi hằng, prefix cho cache, các code type/value chuẩn.
- `Exceptions/`
  - Exception tuỳ chỉnh (vd: `AppException`) cho business rule.
- `AppSettings/`
  - `GoogleAuthSettings`, `JwtSettings`, `EmailSettings`, ...
- `Data/`, `Migrations/`
  - Ngữ cảnh DB (DbContext) và migration EF Core.

**Nguyên tắc**: `BusinessObjects` không phụ thuộc vào các project khác – là lõi dùng chung cho tất cả.

---

### 2.3. `AppBackend.Repositories` – Data Access Layer (DAL)

**Vai trò chính**

- Đóng gói toàn bộ logic truy cập dữ liệu trên EF Core.
- Cung cấp **Generic Repository** và **Repository chuyên biệt** theo entity.
- Cài đặt **Unit of Work** để quản lý transaction xuyên suốt nhiều repo.

**Thành phần chính**

- `Generic/`
  - `IGenericRepository<T>` / `GenericRepository<T>` – các thao tác CRUD cơ bản: `GetByIdAsync`, `FindAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, …
- `Repositories/`
  - Ví dụ: `BookingRepository`, `TransactionRepository`, `AccountRepository`, `RoomRepository`, `CommonCodeRepository`...
  - Có thể chứa các method query đặc thù: `GetByEmailAsync`, `GetByBookingIdAsync`, ...
- `UnitOfWork/`
  - `IUnitOfWork` & `UnitOfWork` – gom tất cả repositories vào một abstraction, expose:
    - `Bookings`, `BookingRooms`, `Rooms`, `RoomTypes`
    - `Transactions`, `CommonCodes`, `BankConfigs`, `Customers`, `Accounts`, `Roles`, ...
    - `SaveChangesAsync()`

**Nguyên tắc**: Services không làm việc trực tiếp với `DbContext`, mà đi qua `UnitOfWork`/Repository để dễ test, dễ thay đổi DB.

---

### 2.4. `AppBackend.Services` – Business Logic Layer (BLL)

**Vai trò chính**

- Chứa toàn bộ **nghiệp vụ lõi** của hệ thống.
- Orchestrate giữa Repositories, Helpers, APIs ngoài (PayOS, Google, Email, VietQR, Gemini chatbot, ...).
- Đảm bảo **transactional consistency**, validate input nâng cao, xử lý workflows phức tạp.

**Cấu trúc chính**

- `Services/`
  - `BookingServices/BookingService`
    - Luồng booking online & guest booking
    - Check availability, lock phòng tạm thời bằng cache
    - Tính giá phòng với ngày lễ (`HolidayPricing`)
    - Tạo `Booking`, `BookingRoom`, `Transaction`
    - Tạo PayOS payment link cho đặt cọc
    - Xử lý webhook PayOS để confirm payment, cập nhật trạng thái booking/transaction, gửi email xác nhận
    - Tự động huỷ booking nếu quá hạn thanh toán mà chưa trả tiền (sử dụng queue & cache)
  - `TransactionServices/TransactionService`
    - Tạo payment / deposit / refund
    - Tạo PayOS payment link cho booking hiện có
    - Tạo QR code VietQR để chuyển khoản
    - Thống kê giao dịch (doanh thu, paid/pending/refunded…) 
  - `Authentication/AuthenticationService`
    - Đăng ký, đăng nhập, logout
    - Tạo JWT & refresh token, lưu refresh token vào cache
    - Reset password, OTP, kích hoạt tài khoản qua email
    - `LoginWithGoogleCallbackAsync(GoogleUserInfo)` – xử lý login Google:
      - Nếu **email chưa tồn tại** → tạo `Account`, `Customer`, gán role `User`
      - Nếu đã tồn tại → đăng nhập, cấp token
  - `Authentication/GoogleLoginService`
    - Thực hiện OAuth2 code flow với Google:
      - Đổi `code` → `access_token` (gọi `https://oauth2.googleapis.com/token`)
      - Lấy user info từ `https://www.googleapis.com/oauth2/v2/userinfo`
      - Decode/normalize dữ liệu trả về (`GoogleUserInfo`)
  - Các service khác: Email, Chatbot (Gemini), Attendance/Payroll, Media/Cloudinary, ...

- `ApiModels/`
  - Request/Response models dành riêng cho API: `CreateBookingRequest`, `CreateGuestBookingRequest`, `ConfirmPaymentRequest`, `CreatePayOSPaymentRequest`, `GenerateQRCodeRequest`, ...

- `Helpers/`
  - `AccountHelper` – hash password, tạo JWT, sinh refresh token, claim roles.
  - `CacheHelper` – wrapper trên Redis/In‑memory cho booking locks, payment caches, refresh token...
  - `QRPaymentHelper` – build VietQR URL, validate config ngân hàng.
  - `BookingTokenHelper` – mã hóa/giải mã token cho booking.
  - Các helper khác: email sending, mapping, message queue, ...

- `MessageQueue/`
  - Định nghĩa `BookingQueueMessage` & `BookingMessageType`.
  - `IBookingQueueService` – enqueue các tác vụ async (create booking, cancel booking do timeout, confirm payment, ...).

**Nguyên tắc**: Mỗi service tập trung vào **một bounded context** (Booking, Transaction, Auth, ...), dễ test & dễ mở rộng.

---

### 2.5. `AppBackend.Extensions` – Infrastructure / AuthConfig

- `AuthConfig.cs`
  - Extension method `AddGoogleAuth`, `AddJwtAuth`, ... (theo comment trong `Program.cs`).
  - Nơi cấu hình AuthenticationScheme, JwtBearerOptions, Google options (nếu dùng middleware của ASP.NET).

Layer này giúp giữ `Program.cs` gọn và gom toàn bộ cấu hình auth vào một chỗ duy nhất.

---

### 2.6. `AppBackend.Tests` – Test Layer

- Chứa các unit test cho Services (ví dụ `BookingServiceTests`).
- Dùng mock `IUnitOfWork`, `PayOS`, `CacheHelper` để test luồng booking & thanh toán mà không gọi thật DB hay PayOS.
- Đảm bảo các refactor không phá vỡ behavior hiện có.

---

## 3. Các luồng nghiệp vụ tiêu biểu (high‑level)

### 3.1. Luồng đặt phòng online + thanh toán cọc qua PayOS

1. **FE** gọi API `POST /api/booking` với `CreateBookingRequest`.
2. `BookingController` → `BookingService.CreateBookingAsync`.
3. `BookingService`:
   - Validate input, ngày check‑in/out.
   - Tìm phòng khả dụng theo từng loại bằng `IsRoomAvailableAsync` + kiểm tra booking đã paid.
   - Lock phòng tạm thời vào cache (để tránh overbooking).
   - Tính tổng tiền (có tính holiday pricing), tạo `Booking`, `BookingRooms`.
   - Tạo `Transaction` pending cho deposit.
   - Gọi PayOS SDK để tạo payment link (qua `PayOS` client) với `expiredAt` ~ 30 phút.
   - Lưu thông tin payment vào cache (`CachePrefix.BookingPayment`).
   - Trả về cho FE `BookingDto` + `PaymentUrl` PayOS.
4. User thanh toán trên PayOS:
   - PayOS gọi **webhook** về API `HandlePayOSWebhookAsync`.
   - Service verify webhook, tìm booking tương ứng qua `orderCode`.
   - Cập nhật `Booking.DepositStatus`, `Transaction.PaymentStatus` thành Paid.
   - Gửi email xác nhận booking.
   - Gửi message vào queue để chính thức giữ phòng, giải phóng lock khỏi cache.
5. Nếu sau X phút (cấu hình) chưa thanh toán:
   - Task background/queue gửi message cancel booking.
   - Hủy booking (nếu chưa có transaction Completed), giải phóng room locks, xóa cache.

Chi tiết hơn có thể xem trong:

- `Docs/BOOKING_SYSTEM_DOCUMENTATION.md`
- `Docs/BOOKING_FLOW_GUIDE.md`
- `Docs/ROOM_AVAILABILITY_LOGIC.md`
- `Docs/CACHE_HANDLING_FOR_PAYMENT_PROCESS.md`

---

### 3.2. Luồng login Google (OAuth2 Code Flow)

1. FE redirect user đến Google OAuth URL (có thể lấy sẵn từ `GET /api/Authentication/google-login-url`).
2. User login & đồng ý scopes → Google redirect về `redirect_uri` (FE) kèm `code`.
3. FE gửi `POST /api/Authentication/exchange-google` với `{ code }`.
4. `AuthenticationController.ExchangeGoogle`:
   - Gọi `GoogleLoginService.GetUserInfoFromCodeAsync(code)` để đổi `code` → `access_token` → `GoogleUserInfo`.
   - Gọi `AuthenticationService.LoginWithGoogleCallbackAsync(userInfo)`:
     - Nếu account chưa tồn tại → tạo account, customer, gán role `User`.
     - Nếu đã tồn tại → đăng nhập.
   - Sinh JWT + refresh token, lưu refresh token vào cache.
   - Set HttpOnly cookies (nếu cùng domain) + trả token & user info trong body.

Chi tiết hơn được mô tả trong:

- `Docs/GOOGLE_LOGIN_FLOW.md`
- `Docs/GOOGLE_LOGIN_QUICK_GUIDE.md`
- `Docs/FRONTEND_GOOGLE_LOGIN_EXAMPLE.md`

---

### 3.3. Thanh toán bằng QR VietQR

1. FE gọi `POST /api/Transaction/generate-qr` (tên API tham khảo) với amount + description.
2. `TransactionService.GenerateQRCodeAsync`:
   - Lấy BankConfig hiện hành từ DB hoặc từ `appsettings:VietQR` (fallback nếu DB chưa có).
   - Validate cấu hình ngân hàng qua `QRPaymentHelper`.
   - Sinh VietQR URL (không upload ảnh) và trả về cho FE.
3. FE hiển thị ảnh QR (do khách tự quét) dựa trên URL trả về.

Chi tiết hơn xem thêm `Docs/ATTENDANCE_PAYROLL_*` và các docs liên quan payment/QR nếu có.

---

## 4. Tích hợp bên ngoài (External Integrations)

Hệ thống tích hợp với một số dịch vụ ngoài:

- **Google OAuth2**
  - Đăng nhập/đăng ký nhanh bằng tài khoản Google.
  - Sử dụng `GoogleAuthSettings` từ `appsettings.json`.
- **PayOS**
  - Tạo payment link cho thanh toán cọc booking.
  - Xử lý webhook thanh toán thành công.
  - Cấu hình trong `PayOS` section của `appsettings.json`.
- **VietQR**
  - Sinh link QR thanh toán ngân hàng theo chuẩn VietQR.
  - Cấu hình qua `VietQR` section hoặc bảng `BankConfig`.
- **Cloudinary**
  - Lưu trữ hình ảnh phòng, khách sạn, hồ sơ.
- **Email (SMTP/Gmail)**
  - Gửi mail xác nhận booking, reset password, kích hoạt tài khoản.
- **Gemini / Google AI**
  - Dùng cho chatbot (theo docs: `GEMINI_API_KEY_MANAGEMENT.md`, `CHATBOT_IMPLEMENTATION_FLOW.md`).

---

## 5. Testing & Observability

- Dự án `AppBackend.Tests` chứa unit test cho services quan trọng (đặc biệt Booking / Payment / Auth).
- `Docs/TESTING_FLOW.md` mô tả cách chạy và tổ chức test manual + API test.
- `AppBackend.ApiCore/ApiTests/` chứa các file `.http` để test luồng booking, transaction, account activation bằng HTTP client (VS Code/JetBrains).
- Logging được thực hiện qua `ILogger<T>` trong services + một số `Console.WriteLine` hỗ trợ debug Google Login/PayOS.

---

## 6. Hướng mở rộng

- Thêm **modular boundaries** rõ ràng hơn (Booking, Billing, Identity) bằng cách tách nhỏ project Services hoặc tách solution.
- Bổ sung **integration tests** cho luồng booking‑payment end‑to‑end.
- Chuẩn hoá logs theo JSON + tích hợp observability stack (Elastic/Seq/Cloud logging).
- Tách `Chatbot`, `Attendance/Payroll` thành bounded context riêng nếu nghiệp vụ phức tạp hơn.

---

> Nếu bạn muốn, có thể tạo thêm sơ đồ sequence cho từng flow (Booking, PayOS, Google Login) dựa trên các docs chi tiết đã có trong thư mục `Docs/`.

