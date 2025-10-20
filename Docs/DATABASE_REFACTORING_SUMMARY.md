# Database Schema Refactoring - Booking System

## ✅ Những thay đổi đã thực hiện

### 1. **Xóa bảng BookingRoomAmenity** ❌
- Bảng này không còn cần thiết
- Logic amenities sẽ được quản lý ở **RoomAmenity** (amenities gắn với loại phòng)
- Không cần track amenities theo từng booking riêng lẻ

### 2. **Booking - Đơn đặt phòng (như Order)** ✅

**CŨ:**
```csharp
public int RoomId { get; set; }  // ❌ Chỉ đặt 1 phòng
public DateTime CheckIn { get; set; }
public DateTime CheckOut { get; set; }
public decimal EstimatedPrice { get; set; }
public int StatusId { get; set; }
```

**MỚI:**
```csharp
public int CustomerId { get; set; }
public DateTime CheckInDate { get; set; }
public DateTime CheckOutDate { get; set; }
public decimal TotalAmount { get; set; }
public decimal DepositAmount { get; set; }
public int? PaymentStatusId { get; set; }
public int? DepositStatusId { get; set; }
public int? BookingTypeId { get; set; }
public string? SpecialRequests { get; set; }

// Navigation
public virtual ICollection<BookingRoom> BookingRooms { get; set; }
public virtual ICollection<Transaction> Transactions { get; set; }
```

**Lợi ích:**
- ✅ Đặt nhiều phòng trong 1 booking
- ✅ Tách rõ TotalAmount vs DepositAmount
- ✅ Track PaymentStatus và DepositStatus riêng
- ✅ Lưu SpecialRequests của khách

### 3. **BookingRoom - Chi tiết đơn (như OrderDetail)** ✅

**CŨ:**
```csharp
public int BookingRoomId { get; set; }
public int BookingId { get; set; }
public int RoomId { get; set; }
public decimal PriceAtTime { get; set; }
public int Quantity { get; set; } = 1;
public int BookedByAccountId { get; set; }  // ❌ Không cần

public virtual ICollection<BookingRoomAmenity> BookingRoomAmenities { get; set; }  // ❌ Xóa
```

**MỚI:**
```csharp
public int BookingRoomId { get; set; }
public int BookingId { get; set; }
public int RoomId { get; set; }

// Pricing details
public decimal PricePerNight { get; set; }  // Giá tại thời điểm đặt
public int NumberOfNights { get; set; }
public decimal SubTotal { get; set; }  // = PricePerNight × NumberOfNights

// Date range
public DateTime CheckInDate { get; set; }
public DateTime CheckOutDate { get; set; }

// Navigation
public virtual ICollection<BookingRoomService> BookingRoomServices { get; set; }
```

**Lợi ích:**
- ✅ Giống OrderDetail pattern (rõ ràng, dễ hiểu)
- ✅ Lưu giá tại thời điểm đặt (immutable pricing)
- ✅ Tính toán tổng tiền dễ dàng: `SubTotal = PricePerNight × NumberOfNights`
- ✅ Xóa `BookedByAccountId` - không cần thiết vì Booking đã có CreatedBy
- ✅ Xóa `BookingRoomAmenities` collection

### 4. **Transaction - Giao dịch thanh toán** ✅

```csharp
public int TransactionId { get; set; }
public int BookingId { get; set; }
public decimal TotalAmount { get; set; }
public decimal PaidAmount { get; set; }
public decimal? DepositAmount { get; set; }
public int PaymentMethodId { get; set; }
public int PaymentStatusId { get; set; }
public int TransactionStatusId { get; set; }
public string? OrderCode { get; set; }  // PayOS order code
public DateTime? DepositDate { get; set; }
```

**Tích hợp PayOS:**
- Tạo Transaction khi tạo Booking
- Lưu `OrderCode` từ PayOS
- Track trạng thái thanh toán

### 5. **Amenity** ✅

**Xóa:**
```csharp
public virtual ICollection<BookingRoomAmenity> BookingRoomAmenities { get; set; }  // ❌ REMOVED
```

**Giữ lại:**
```csharp
public virtual ICollection<RoomAmenity> RoomAmenities { get; set; }  // ✅ OK
```

## 🔄 Luồng đặt phòng mới

### Tạo Booking:
```
1. User chọn phòng: [101, 102, 201]
2. CheckIn: 2025-10-20, CheckOut: 2025-10-22 (2 đêm)

3. Tạo Booking:
   - TotalAmount = (800k×2 + 800k×2 + 1500k×2) = 6,200,000 VND
   - DepositAmount = 6,200,000 × 0.3 = 1,860,000 VND

4. Tạo BookingRoom records (OrderDetail):
   - BookingRoom #1: Room 101, 800k/đêm, 2 đêm, SubTotal: 1,600k
   - BookingRoom #2: Room 102, 800k/đêm, 2 đêm, SubTotal: 1,600k
   - BookingRoom #3: Room 201, 1500k/đêm, 2 đêm, SubTotal: 3,000k

5. Tạo Transaction:
   - TotalAmount: 6,200,000
   - DepositAmount: 1,860,000
   - PaidAmount: 0 (chưa thanh toán)
   - OrderCode: từ PayOS

6. Return Payment URL để khách thanh toán
```

## 📊 Database Relations

```
Customer
   ↓ 1:N
Booking (đơn đặt phòng)
   ↓ 1:N
BookingRoom (chi tiết phòng)
   ↓ N:1
Room

Booking
   ↓ 1:N
Transaction (thanh toán)
```

## 🗄️ Repositories đã tạo/cập nhật

1. ✅ **BookingRepository** - đơn giản hóa
2. ✅ **BookingRoomRepository** - đơn giản hóa, xóa `IsRoomBookedAsync`
3. ✅ **TransactionRepository** - mới tạo
4. ✅ **UnitOfWork** - thêm `Bookings`, `BookingRooms`, `Transactions`

## 🔧 Migration cần thực hiện

```sql
-- 1. Drop bảng BookingRoomAmenity
DROP TABLE IF EXISTS BookingRoomAmenity;

-- 2. Alter bảng Booking
ALTER TABLE Booking
DROP COLUMN RoomId,
DROP COLUMN CheckIn,
DROP COLUMN CheckOut,
DROP COLUMN EstimatedPrice,
DROP COLUMN StatusId,
DROP COLUMN Notes;

ALTER TABLE Booking
ADD CheckInDate datetime NOT NULL,
ADD CheckOutDate datetime NOT NULL,
ADD TotalAmount decimal(18,2) NOT NULL,
ADD DepositAmount decimal(18,2) NOT NULL,
ADD PaymentStatusId int NULL,
ADD DepositStatusId int NULL,
ADD BookingTypeId int NULL,
ADD SpecialRequests nvarchar(500) NULL;

-- 3. Alter bảng BookingRoom
ALTER TABLE BookingRoom
DROP COLUMN PriceAtTime,
DROP COLUMN Quantity,
DROP COLUMN BookedByAccountId;

ALTER TABLE BookingRoom
ADD PricePerNight decimal(18,2) NOT NULL,
ADD NumberOfNights int NOT NULL,
ADD SubTotal decimal(18,2) NOT NULL,
ADD CheckInDate datetime NOT NULL,
ADD CheckOutDate datetime NOT NULL;
```

## ✅ Hoàn thành

Hệ thống đã được refactor hoàn toàn theo pattern chuẩn:
- ✅ **Booking** = Order
- ✅ **BookingRoom** = OrderDetail  
- ✅ **Transaction** = Payment
- ✅ Xóa `BookingRoomAmenity` (không cần)
- ✅ Xóa `BookedByAccountId` (không cần)
- ✅ Logic đặt phòng rõ ràng, dễ bảo trì
- ✅ Tích hợp PayOS hoàn chỉnh
- ✅ Room locking với cache
- ✅ Message queue processing

