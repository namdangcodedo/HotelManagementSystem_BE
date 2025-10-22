# Room Availability Logic - Logic Kiểm Tra Phòng Trống

## Tổng Quan Thay Đổi

### ❌ Cách Cũ (SAI)
- Giá lưu ở từng **Room** (phòng 101, 102...)
- Không biết cách check số lượng phòng available
- Logic không hợp lý với thực tế khách sạn

### ✅ Cách Mới (ĐÚNG)
- Giá lưu ở **RoomType** (Standard, Deluxe, Suite...)
- Room chỉ lưu thông tin định danh (số phòng, trạng thái)
- Check availability qua BookingRoom với CheckInDate/CheckOutDate

---

## Cấu Trúc Models Mới

### 1. RoomType (Loại Phòng)
```csharp
public class RoomType
{
  public int RoomTypeId { get; set; }
  public string TypeName { get; set; }        // "Standard Room", "Deluxe Room"
  public string TypeCode { get; set; }        // "STD", "DLX", "SUI"
  public decimal BasePriceNight { get; set; } // 1,000,000 VNĐ/đêm
  public int MaxOccupancy { get; set; }       // 2 người
  public decimal? RoomSize { get; set; }      // 25 m2
  public int? NumberOfBeds { get; set; }      // 1 giường
  public string? BedType { get; set; }        // "King Size"
  // ...
}
```

**Ý nghĩa:** 
- Lưu thông tin chung của loại phòng
- **Giá cả ở đây** - tất cả phòng cùng loại có cùng giá
- Thông tin hiển thị cho khách hàng

### 2. Room (Phòng Cụ Thể)
```csharp
public class Room
{
  public int RoomId { get; set; }
  public string RoomNumber { get; set; }      // "101", "102", "A-201"
  public int RoomTypeId { get; set; }         // FK -> RoomType
  public int StatusId { get; set; }           // Available, Occupied, Cleaning...
  public int? FloorNumber { get; set; }       // Tầng 1, 2, 3...
  // KHÔNG CÓ GIÁ Ở ĐÂY!
}
```

**Ý nghĩa:**
- Phòng vật lý trong khách sạn
- Chỉ lưu thông tin định danh
- Trạng thái hiện tại của phòng

### 3. BookingRoom (Chi Tiết Đặt Phòng)
```csharp
public class BookingRoom
{
  public int BookingRoomId { get; set; }
  public int BookingId { get; set; }
  public int RoomId { get; set; }
  public DateTime CheckInDate { get; set; }
  public DateTime CheckOutDate { get; set; }
  public decimal PricePerNight { get; set; }  // Giá tại thời điểm đặt
  public int NumberOfNights { get; set; }
  public decimal SubTotal { get; set; }
}
```

**Ý nghĩa:**
- Lưu lịch sử booking của từng phòng
- **Dùng để check availability**
- Lưu giá tại thời điểm đặt (có thể khác giá hiện tại)

---

## Logic Check Availability

### Kịch Bản: Khách muốn đặt phòng từ 2025-10-25 đến 2025-10-27

### Bước 1: Hiển thị RoomType cho khách chọn
```sql
SELECT 
  rt.RoomTypeId,
  rt.TypeName,
  rt.BasePriceNight,
  rt.MaxOccupancy,
  COUNT(r.RoomId) AS TotalRooms
FROM RoomType rt
LEFT JOIN Room r ON r.RoomTypeId = rt.RoomTypeId
WHERE rt.IsActive = 1
GROUP BY rt.RoomTypeId
```

**Kết quả hiển thị:**
```
RoomType          | BasePriceNight | TotalRooms
------------------|----------------|------------
Standard Room     | 1,000,000      | 10
Deluxe Room       | 1,500,000      | 5
Suite             | 3,000,000      | 2
```

### Bước 2: Check số phòng available theo loại
Khi khách chọn "Standard Room", check xem còn bao nhiêu phòng trống:

```csharp
public async Task<int> GetAvailableRoomCount(
    int roomTypeId, 
    DateTime checkIn, 
    DateTime checkOut)
{
    // 1. Tổng số phòng của loại này
    var totalRooms = await _context.Rooms
        .Where(r => r.RoomTypeId == roomTypeId)
        .CountAsync();

    // 2. Số phòng đã được đặt trong khoảng thời gian này
    var bookedRooms = await _context.BookingRooms
        .Where(br => 
            br.Room.RoomTypeId == roomTypeId &&
            // Kiểm tra overlap thời gian
            !(br.CheckOutDate <= checkIn || br.CheckInDate >= checkOut)
        )
        .Select(br => br.RoomId)
        .Distinct()
        .CountAsync();

    // 3. Số phòng available = Tổng - Đã đặt
    return totalRooms - bookedRooms;
}
```

**Giải thích logic overlap:**
```
Booking đã tồn tại:  [====]
CheckIn/Out mới:           [====]  ✅ OK (không overlap)

Booking đã tồn tại:  [========]
CheckIn/Out mới:        [====]     ❌ OVERLAP

Booking đã tồn tại:     [====]
CheckIn/Out mới:     [========]    ❌ OVERLAP
```

### Bước 3: Chọn phòng cụ thể để đặt
```csharp
public async Task<List<Room>> GetAvailableRooms(
    int roomTypeId, 
    DateTime checkIn, 
    DateTime checkOut)
{
    // Lấy tất cả phòng của loại này
    var allRooms = await _context.Rooms
        .Where(r => r.RoomTypeId == roomTypeId)
        .ToListAsync();

    // Lấy danh sách RoomId đã được đặt
    var bookedRoomIds = await _context.BookingRooms
        .Where(br => 
            br.Room.RoomTypeId == roomTypeId &&
            !(br.CheckOutDate <= checkIn || br.CheckInDate >= checkOut)
        )
        .Select(br => br.RoomId)
        .Distinct()
        .ToListAsync();

    // Trả về phòng chưa được đặt
    return allRooms
        .Where(r => !bookedRoomIds.Contains(r.RoomId))
        .ToList();
}
```

---

## Flow Booking Mới

### 1. **Khách xem danh sách loại phòng**
```
GET /api/room-types?checkIn=2025-10-25&checkOut=2025-10-27

Response:
[
  {
    "roomTypeId": 1,
    "typeName": "Standard Room",
    "pricePerNight": 1000000,
    "availableCount": 7,  // 10 phòng - 3 đã đặt
    "totalRooms": 10
  },
  {
    "roomTypeId": 2,
    "typeName": "Deluxe Room",
    "pricePerNight": 1500000,
    "availableCount": 5,  // Tất cả đều trống
    "totalRooms": 5
  }
]
```

### 2. **Khách chọn loại phòng và số lượng**
```json
{
  "roomTypeId": 1,
  "quantity": 2,  // Đặt 2 phòng Standard
  "checkIn": "2025-10-25",
  "checkOut": "2025-10-27",
  "guests": 4
}
```

### 3. **Backend check và tự động chọn phòng**
```csharp
// 1. Check còn đủ phòng không?
var availableRooms = await GetAvailableRooms(
    roomTypeId: 1, 
    checkIn, 
    checkOut
);

if (availableRooms.Count < 2)
{
    throw new Exception("Không đủ phòng trống!");
}

// 2. Tự động chọn 2 phòng
var selectedRooms = availableRooms.Take(2).ToList();
// Ví dụ: Room 101, Room 102

// 3. Lấy giá từ RoomType
var roomType = await _context.RoomTypes
    .FindAsync(roomTypeId);

// 4. Tạo booking
var booking = new Booking
{
    CustomerId = customerId,
    CheckInDate = checkIn,
    CheckOutDate = checkOut,
    // ...
};

// 5. Tạo BookingRoom cho từng phòng
foreach (var room in selectedRooms)
{
    var bookingRoom = new BookingRoom
    {
        BookingId = booking.BookingId,
        RoomId = room.RoomId,
        CheckInDate = checkIn,
        CheckOutDate = checkOut,
        PricePerNight = roomType.BasePriceNight,  // Lấy từ RoomType
        NumberOfNights = (checkOut - checkIn).Days,
        SubTotal = roomType.BasePriceNight * (checkOut - checkIn).Days
    };
    
    await _context.BookingRooms.AddAsync(bookingRoom);
}
```

---

## Ví Dụ Thực Tế

### Dữ liệu mẫu:

**RoomType:**
| RoomTypeId | TypeName      | BasePriceNight |
|------------|---------------|----------------|
| 1          | Standard Room | 1,000,000      |
| 2          | Deluxe Room   | 1,500,000      |

**Room:**
| RoomId | RoomNumber | RoomTypeId |
|--------|------------|------------|
| 1      | 101        | 1          |
| 2      | 102        | 1          |
| 3      | 103        | 1          |
| 4      | 201        | 2          |
| 5      | 202        | 2          |

**BookingRoom (hiện tại):**
| RoomId | CheckInDate | CheckOutDate |
|--------|-------------|--------------|
| 1      | 2025-10-24  | 2025-10-26   |
| 2      | 2025-10-25  | 2025-10-28   |

### Query: Đặt Standard Room từ 2025-10-25 đến 2025-10-27

**Phòng đã đặt:**
- Room 101: 24-26 → ❌ OVERLAP với 25-27
- Room 102: 25-28 → ❌ OVERLAP với 25-27
- Room 103: Không có booking → ✅ AVAILABLE

**Kết quả:** Còn 1 phòng Standard trống (Room 103)

---

## Lợi Ích Của Thiết Kế Mới

### ✅ Đúng với thực tế khách sạn
- Khách chọn **loại phòng**, không phải phòng cụ thể
- Giá theo **loại**, không phải từng phòng

### ✅ Dễ quản lý giá
- Muốn tăng giá Standard Room? Chỉ sửa 1 record trong RoomType
- Không cần sửa 10 records trong Room

### ✅ Dễ check availability
- Query đơn giản, hiệu quả
- Biết chính xác số lượng phòng trống

### ✅ Flexible
- Dễ thêm pricing rules (weekend, holiday...)
- Dễ implement dynamic pricing

### ✅ Lưu lịch sử giá
- BookingRoom lưu giá tại thời điểm đặt
- Thay đổi giá hiện tại không ảnh hưởng booking cũ

---

## Migration & Update Data

### 1. Tạo bảng RoomType
```sql
CREATE TABLE RoomType (
    RoomTypeId INT PRIMARY KEY IDENTITY(1,1),
    TypeName NVARCHAR(100) NOT NULL,
    TypeCode NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    BasePriceNight DECIMAL(18,2) NOT NULL,
    MaxOccupancy INT NOT NULL,
    RoomSize DECIMAL(10,2),
    NumberOfBeds INT,
    BedType NVARCHAR(50),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedAt DATETIME2,
    UpdatedBy INT
);
```

### 2. Migrate data từ CommonCode
```sql
-- Tạo RoomType từ CommonCode
INSERT INTO RoomType (TypeName, TypeCode, BasePriceNight, MaxOccupancy, IsActive, CreatedAt)
SELECT 
    CodeName,           -- "Standard Room"
    CodeValue,          -- "STD"
    1000000,            -- Giá mặc định, cần update sau
    2,                  -- Mặc định 2 người
    IsActive,
    GETDATE()
FROM CommonCode
WHERE CodeType = 'ROOM_TYPE';
```

### 3. Update Room table
```sql
-- Thêm cột RoomTypeId
ALTER TABLE Room ADD RoomTypeId INT;

-- Map từ RoomTypeCode (CommonCode) sang RoomTypeId
UPDATE r
SET r.RoomTypeId = rt.RoomTypeId
FROM Room r
INNER JOIN CommonCode cc ON r.RoomTypeCodeId = cc.CodeId
INNER JOIN RoomType rt ON rt.TypeCode = cc.CodeValue;

-- Xóa các cột không cần thiết
ALTER TABLE Room DROP COLUMN BasePricePerNight;
ALTER TABLE Room DROP COLUMN RoomTypeCodeId;
```

---

## API Endpoints Mới

### 1. Xem loại phòng available
```
GET /api/room-types/available?checkIn=2025-10-25&checkOut=2025-10-27
```

### 2. Xem chi tiết loại phòng
```
GET /api/room-types/{id}?checkIn=2025-10-25&checkOut=2025-10-27
```

### 3. Đặt phòng
```
POST /api/bookings
{
  "roomTypeId": 1,
  "quantity": 2,
  "checkIn": "2025-10-25",
  "checkOut": "2025-10-27"
}
```

---

## Next Steps

1. ✅ Tạo model RoomType
2. ✅ Update model Room
3. ⏳ Update DbContext
4. ⏳ Tạo migration
5. ⏳ Update repositories
6. ⏳ Update services
7. ⏳ Update controllers
8. ⏳ Update DTOs
9. ⏳ Test availability logic

---

**Tài liệu được tạo:** 2025-10-22
**Tác giả:** Development Team

