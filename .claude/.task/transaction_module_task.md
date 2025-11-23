# Task: Transaction Module API Development

## Module Overview
**Transaction Module** quản lý tất cả các giao dịch tài chính trong hệ thống khách sạn, bao gồm:
- Thanh toán booking (Payment)
- Tiền đặt cọc (Deposit)
- Hoàn tiền (Refund)
- Lịch sử giao dịch
- Phương thức thanh toán tại quầy: Tiền mặt, Thẻ, QR Bank

## Business Requirements
- Quản lý trạng thái thanh toán (Paid, Unpaid, Refunded, v.v.)
- Xử lý tiền cọc cho booking
- Phương thức thanh toán tại quầy: Cash, Card, QR
- Config QR Bank với tên ngân hàng và số tài khoản
- Tạo QR code cho thanh toán
- Bảo mật và audit trail

## Required APIs Analysis

### 1. Payment Management APIs
#### Create Payment
- **Endpoint:** `POST /api/payment/create`
- **Purpose:** Tạo giao dịch thanh toán mới cho booking
- **Request Body:**
  ```json
  {
    "bookingId": 123,
    "amount": 1000000,
    "paymentMethod": "QR",
    "description": "Thanh toán booking phòng Deluxe"
  }
  ```
- **Response:** Payment ID, status, QR code URL (nếu QR)

#### Update Payment Status
- **Endpoint:** `PUT /api/payment/{id}/status`
- **Purpose:** Cập nhật trạng thái thanh toán (Paid, Refunded, v.v.)
- **Request Body:**
  ```json
  {
    "status": "Paid",
    "transactionId": "TXN123456"
  }
  ```

#### Get Payment Details
- **Endpoint:** `GET /api/payment/{id}`
- **Purpose:** Lấy chi tiết giao dịch thanh toán

#### Get Payment History
- **Endpoint:** `GET /api/payment/history`
- **Purpose:** Lấy lịch sử thanh toán của user/booking
- **Query Params:** userId, bookingId, dateFrom, dateTo

### 2. Deposit Management APIs
#### Create Deposit
- **Endpoint:** `POST /api/deposit/create`
- **Purpose:** Tạo tiền đặt cọc cho booking
- **Request Body:**
  ```json
  {
    "bookingId": 123,
    "amount": 500000,
    "paymentMethod": "Cash"
  }
  ```

#### Refund Deposit
- **Endpoint:** `POST /api/deposit/{id}/refund`
- **Purpose:** Hoàn tiền cọc khi hủy booking
- **Request Body:**
  ```json
  {
    "reason": "Customer cancelled",
    "refundAmount": 500000
  }
  ```

#### Get Deposit Status
- **Endpoint:** `GET /api/deposit/{id}/status`
- **Purpose:** Kiểm tra trạng thái tiền cọc

### 3. Refund Management APIs
#### Process Refund
- **Endpoint:** `POST /api/refund/process`
- **Purpose:** Xử lý hoàn tiền
- **Request Body:**
  ```json
  {
    "paymentId": 456,
    "refundAmount": 200000,
    "reason": "Partial refund due to changes"
  }
  ```

#### Get Refund History
- **Endpoint:** `GET /api/refund/history`
- **Purpose:** Lịch sử hoàn tiền

### 4. Payment Method APIs
#### Get Available Payment Methods
- **Endpoint:** `GET /api/payment/methods`
- **Purpose:** Lấy danh sách phương thức thanh toán khả dụng (Cash, Card, QR từ CommonCode)

#### Validate Payment Method
- **Endpoint:** `POST /api/payment/method/validate`
- **Purpose:** Kiểm tra tính hợp lệ của phương thức thanh toán

### 5. QR Payment APIs
#### Generate QR Code
- **Endpoint:** `POST /api/payment/qr/generate`
- **Purpose:** Tạo QR code cho thanh toán
- **Request Body:**
  ```json
  {
    "amount": 1000000,
    "description": "Thanh toán booking"
  }
  ```
- **Response:** QR code image URL hoặc data

#### Get Bank Config
- **Endpoint:** `GET /api/payment/bank/config`
- **Purpose:** Lấy config ngân hàng (tên ngân hàng, số tài khoản) cho QR

#### Update Bank Config
- **Endpoint:** `PUT /api/payment/bank/config`
- **Purpose:** Cập nhật config ngân hàng (chỉ Admin)
- **Request Body:**
  ```json
  {
    "bankName": "Vietcombank",
    "accountNumber": "1234567890"
  }
  ```

#### Verify QR Payment
- **Endpoint:** `POST /api/payment/qr/verify`
- **Purpose:** Xác nhận thanh toán qua QR (manual check hoặc auto từ bank callback nếu có)

## Database Schema Requirements
- **Payment Table:** Id, BookingId, Amount, StatusId, MethodId, TransactionId, CreatedAt, UpdatedAt
- **Deposit Table:** Id, BookingId, Amount, StatusId, RefundedAt
- **Refund Table:** Id, PaymentId, Amount, Reason, ProcessedAt
- **BankConfig Table:** Id, BankName, AccountNumber, IsActive
- Sử dụng CommonCode cho Status (PaymentStatus, DepositStatus), Method (Cash, Card, QR)

## Security Considerations
- Authentication required for all APIs
- Role-based access (Receptionist for payments, Admin for config)
- Audit logging for all financial transactions
- Data encryption for sensitive payment info

## Implementation Steps
1. Tạo Models: Payment, Deposit, Refund, BankConfig
2. Tạo DTOs cho request/response
3. Implement Services: PaymentService, DepositService, RefundService, QRService
4. Tạo Controllers với endpoints trên
5. Tích hợp QR generation (VietQR library)
6. Unit tests cho tất cả logic
7. Documentation với Swagger

## Dependencies
- VietQR library for QR code generation
- EF Core for data access
- AutoMapper for DTO mapping
- FluentValidation for request validation

---
Task này định nghĩa đầy đủ các API cần thiết để quản lý module transaction tại quầy lễ tân với phương thức Cash, Card, QR.
